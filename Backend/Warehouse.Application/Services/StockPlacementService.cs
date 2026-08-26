using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class StockPlacementService : IStockPlacementService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockPlacementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string?> PlaceAsync(
        Product product,
        Location location,
        int quantity,
        string userId,
        StockTransactionType transactionType)
    {
        var productId = product.Id;

        // Row lock on the destination FIRST, before reading or writing anything else in
        // this transaction — without it, two concurrent placements into the same near-full
        // location could both read "room for one more" before either commits. A second
        // concurrent caller targeting this location blocks here until we commit or roll back.
        await _unitOfWork.Locations.LockForUpdateAsync(location.Id);

        // Find-or-create respects the unique (ProductId, LocationId) index on Stock — this
        // may be a brand-new pairing if the worker chose a location the product has never
        // been stored in.
        var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(productId, location.Id);

        // A SKU already here with a non-zero quantity doesn't newly occupy a slot —
        // including one whose Stock row sits at 0 (previously here, now empty): that is
        // checked exactly like a brand-new SKU, not silently exempted.
        var alreadyOccupiesSlot = stock != null && stock.PhysicalQuantity > 0;

        if (!alreadyOccupiesSlot)
        {
            var limit = location.MaxDistinctSkus ?? LocationCapacityDefaults.GetDefaultMaxDistinctSkus(location.Type);
            if (limit.HasValue)
            {
                var currentDistinctSkuCount = await _unitOfWork.Stocks.CountDistinctProductsWithStockAtLocationAsync(location.Id);
                if (currentDistinctSkuCount >= limit.Value)
                {
                    return $"Location '{location.AddressBarcode}' already stocks {currentDistinctSkuCount}/{limit.Value} distinct SKUs " +
                           $"and doesn't currently stock {product.Sku} — choose a different location.";
                }
            }
        }

        if (stock == null)
        {
            stock = new Stock
            {
                ProductId = productId,
                LocationId = location.Id,
                PhysicalQuantity = 0,
                ReservedQuantity = 0
            };
            _unitOfWork.Stocks.Add(stock);
        }

        stock.PhysicalQuantity += quantity;

        _unitOfWork.StockTransactions.Add(new StockTransaction
        {
            ProductId = productId,
            LocationId = location.Id,
            QuantityChange = quantity,
            TransactionType = transactionType,
            UserId = userId
        });

        return null;
    }
}
