using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class RelocationService : IRelocationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockPlacementService _stockPlacement;

    public RelocationService(IUnitOfWork unitOfWork, IStockPlacementService stockPlacement)
    {
        _unitOfWork = unitOfWork;
        _stockPlacement = stockPlacement;
    }

    public async Task<RelocationStateDto> GetStateAsync(string workerId, string displayName)
    {
        var transit = await _unitOfWork.Locations.GetOrCreateTransitForWorkerAsync(workerId, displayName);
        return await BuildStateAsync(transit);
    }

    public async Task<Result<LocationContentsDto>> GetLocationContentsAsync(string locationBarcode)
    {
        var location = await _unitOfWork.Locations.GetByBarcodeAsync(locationBarcode);
        if (location == null)
            return Result<LocationContentsDto>.Failure($"Location '{locationBarcode}' was not found.", ResultErrorType.NotFound);

        if (location.Type == LocationType.Transit)
            return Result<LocationContentsDto>.Failure("That's a transit location, not a shelf.");

        var stocks = await _unitOfWork.Stocks.GetWithProductAtLocationAsync(location.Id);

        return Result<LocationContentsDto>.Success(new LocationContentsDto
        {
            LocationBarcode = location.AddressBarcode,
            Items = stocks.Select(ToLine).ToList()
        });
    }

    public async Task<Result<RelocationStateDto>> TakeAsync(string workerId, string displayName, RelocationTakeDto dto)
    {
        if (dto.Quantity <= 0)
            return Result<RelocationStateDto>.Failure("Quantity must be greater than zero.");

        var source = await _unitOfWork.Locations.GetByBarcodeAsync(dto.SourceLocationBarcode);
        if (source == null)
            return Result<RelocationStateDto>.Failure($"Location '{dto.SourceLocationBarcode}' was not found.", ResultErrorType.NotFound);

        // Taking FROM a transit location is never right: your own is emptied by putting
        // away, and someone else's isn't yours to take from.
        if (source.Type == LocationType.Transit)
            return Result<RelocationStateDto>.Failure("Cannot take from a transit location — put away what you're carrying instead.");

        var product = await ResolveProductAsync(dto.ProductSku);
        if (product == null)
            return Result<RelocationStateDto>.Failure($"Product '{dto.ProductSku}' was not found.", ResultErrorType.NotFound);

        // Resolved BEFORE the transaction opens: this may create the row, and a failed
        // insert inside a Postgres transaction aborts it (see GetOrCreateTransitForWorkerAsync).
        var transit = await _unitOfWork.Locations.GetOrCreateTransitForWorkerAsync(workerId, displayName);

        var failure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Lock the source row FIRST and take the committed quantities from the lock,
            // not from any earlier read. Two workers taking the same SKU off the same shelf
            // would otherwise both see the same availability; the loser would be stopped
            // only by Stock's xmin token and told "changed by someone else" rather than
            // how much is actually left. Blocking (not SKIP LOCKED) is right here: these
            // workers want this specific row, so the second should wait and then see the
            // truth — same reasoning as the container claim.
            var locked = await _unitOfWork.Stocks.LockForUpdateAsync(product.Id, source.Id);
            if (locked == null)
                return $"No stock of {product.Sku} at '{source.AddressBarcode}'.";

            var (physical, reserved) = locked.Value;
            var available = physical - reserved;

            if (available <= 0)
            {
                return reserved > 0
                    ? $"All {physical} unit(s) of {product.Sku} here are reserved for a pick task and cannot be relocated."
                    : $"No stock of {product.Sku} at '{source.AddressBarcode}'.";
            }

            if (dto.Quantity > available)
            {
                return reserved > 0
                    ? $"Only {available} unit(s) of {product.Sku} can be relocated from here — {reserved} of the {physical} on the shelf are reserved for a pick task."
                    : $"Only {available} unit(s) of {product.Sku} are on the shelf here.";
            }

            var sourceStock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(product.Id, source.Id);
            if (sourceStock == null)
                return $"No stock of {product.Sku} at '{source.AddressBarcode}'.";

            // Assigned from the LOCKED value rather than decremented in place, so the
            // result can't be built on a stale tracked instance from earlier in the request.
            sourceStock.PhysicalQuantity = physical - dto.Quantity;

            _unitOfWork.StockTransactions.Add(new StockTransaction
            {
                ProductId = product.Id,
                LocationId = source.Id,
                QuantityChange = -dto.Quantity,
                TransactionType = StockTransactionType.Relocation,
                UserId = workerId
            });

            // Into the worker's hands. Same placement path as putaway, so the audit row and
            // find-or-create behave identically; the capacity check is a no-op here because
            // a transit location has no distinct-SKU limit.
            var placementFailure = await _stockPlacement.PlaceAsync(
                product, transit, dto.Quantity, workerId, StockTransactionType.Relocation);

            if (placementFailure != null)
                return placementFailure;

            await _unitOfWork.SaveChangesAsync();
            return (string?)null;
        });

        if (failure != null)
            return Result<RelocationStateDto>.Failure(failure, ResultErrorType.Conflict);

        return Result<RelocationStateDto>.Success(await BuildStateAsync(transit));
    }

    public async Task<Result<RelocationStateDto>> PutAwayAsync(string workerId, string displayName, RelocationPutawayDto dto)
    {
        if (dto.Quantity <= 0)
            return Result<RelocationStateDto>.Failure("Quantity must be greater than zero.");

        var target = await _unitOfWork.Locations.GetByBarcodeAsync(dto.TargetLocationBarcode);
        if (target == null)
            return Result<RelocationStateDto>.Failure($"Location '{dto.TargetLocationBarcode}' was not found.", ResultErrorType.NotFound);

        if (target.Type == LocationType.Transit)
            return Result<RelocationStateDto>.Failure("Cannot put away into a transit location — scan a real shelf location.");

        var product = await ResolveProductAsync(dto.ProductSku);
        if (product == null)
            return Result<RelocationStateDto>.Failure($"Product '{dto.ProductSku}' was not found.", ResultErrorType.NotFound);

        var transit = await _unitOfWork.Locations.GetOrCreateTransitForWorkerAsync(workerId, displayName);

        var failure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Locked for the same reason as the source leg, though contention is far less
            // likely here: only this worker's own requests touch their transit location, so
            // this really guards double-submits (an impatient second tap on Confirm).
            var locked = await _unitOfWork.Stocks.LockForUpdateAsync(product.Id, transit.Id);
            if (locked == null || locked.Value.PhysicalQuantity <= 0)
                return $"You aren't carrying any {product.Sku}.";

            var carried = locked.Value.PhysicalQuantity;

            if (dto.Quantity > carried)
                return $"You're only carrying {carried} unit(s) of {product.Sku}.";

            var transitStock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(product.Id, transit.Id);
            if (transitStock == null)
                return $"You aren't carrying any {product.Sku}.";

            // Placement first: it can refuse on MaxDistinctSkus, and refusing after the
            // units have left the worker's hands would strand them. The whole transaction
            // rolls back on failure either way, but ordering it this way keeps the failure
            // path obvious rather than relying on the rollback to undo a half-move.
            var placementFailure = await _stockPlacement.PlaceAsync(
                product, target, dto.Quantity, workerId, StockTransactionType.Relocation);

            if (placementFailure != null)
                return placementFailure;

            transitStock.PhysicalQuantity = carried - dto.Quantity;

            _unitOfWork.StockTransactions.Add(new StockTransaction
            {
                ProductId = product.Id,
                LocationId = transit.Id,
                QuantityChange = -dto.Quantity,
                TransactionType = StockTransactionType.Relocation,
                UserId = workerId
            });

            await _unitOfWork.SaveChangesAsync();
            return (string?)null;
        });

        if (failure != null)
            return Result<RelocationStateDto>.Failure(failure, ResultErrorType.Conflict);

        return Result<RelocationStateDto>.Success(await BuildStateAsync(transit));
    }

    // Reuses the batched lookup with a single SKU rather than adding a near-duplicate
    // single-SKU repository method — one query either way.
    private async Task<Product?> ResolveProductAsync(string sku)
    {
        var bySku = await _unitOfWork.Products.GetBySkusAsync(new List<string> { sku });
        return bySku.TryGetValue(sku, out var product) ? product : null;
    }

    private async Task<RelocationStateDto> BuildStateAsync(Location transit)
    {
        var carried = await _unitOfWork.Stocks.GetWithProductAtLocationAsync(transit.Id);

        return new RelocationStateDto
        {
            TransitBarcode = transit.AddressBarcode,
            CarriedItems = carried.Select(ToLine).ToList(),

            // The exit guard: a worker cannot walk away holding stock.
            //
            // KNOWN GAP — this is a UI rule, and a UI rule does not survive a dead session.
            // A worker whose battery dies, who loses signal, or who simply closes the app
            // mid-relocation leaves stock sitting in their transit location indefinitely.
            // It is invisible to order allocation (every stock query now excludes Transit
            // by design, so it can't be reserved or picked) and appears in no screen that
            // anyone would think to check, so nobody finds out until a cycle count.
            //
            // Missing piece: a supervisor view of non-empty transit locations plus a
            // force-return that moves the stock back to a real location. Deliberately not
            // built here.
            //
            // This is the fourth dead-end state of this shape in the codebase — orders
            // stuck in Picking (see CancelPickTaskAsync), containers stuck in Ready,
            // pick tasks stuck claimed (solved by an inactivity sweep), and now this. The
            // pattern is always the same: state a worker owns, released only by a happy
            // path the worker might never reach. The sweep in PickTaskService.GetNextTaskAsync
            // is the shape of the eventual answer here too.
            CanExit = carried.Count == 0
        };
    }

    private static RelocationStockLineDto ToLine(Stock stock) => new()
    {
        ProductSku = stock.Product!.Sku,
        ProductName = stock.Product!.Name,
        PhysicalQuantity = stock.PhysicalQuantity,
        ReservedQuantity = stock.ReservedQuantity,
        AvailableQuantity = stock.AvailableQuantity
    };
}
