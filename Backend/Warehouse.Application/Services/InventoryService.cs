using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StockAdjustmentResultDto>> AdjustPhysicalStockAsync(Guid productId, string locationBarcode, int quantityDelta, string reason, bool confirmReservationImpact, string userId)
    {
        if (quantityDelta == 0)
            return Result<StockAdjustmentResultDto>.Failure("Quantity delta must not be zero.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result<StockAdjustmentResultDto>.Failure("A reason is required for a manual stock adjustment.");

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            return Result<StockAdjustmentResultDto>.Failure($"Product {productId} was not found.", ResultErrorType.NotFound);

        var location = await _unitOfWork.Locations.GetByBarcodeAsync(locationBarcode);
        if (location == null)
            return Result<StockAdjustmentResultDto>.Failure($"Location '{locationBarcode}' was not found.", ResultErrorType.NotFound);

        var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(productId, location.Id);

        if (stock == null)
        {
            if (quantityDelta < 0)
                return Result<StockAdjustmentResultDto>.Failure("There is no existing stock at this location to remove from.");

            stock = new Stock
            {
                ProductId = productId,
                LocationId = location.Id,
                PhysicalQuantity = 0,
                ReservedQuantity = 0
            };
            _unitOfWork.Stocks.Add(stock);
        }

        var newPhysicalQuantity = stock.PhysicalQuantity + quantityDelta;
        if (newPhysicalQuantity < 0)
            return Result<StockAdjustmentResultDto>.Failure(
                $"Adjustment would take physical quantity negative (currently {stock.PhysicalQuantity}, delta {quantityDelta}).");

        // A cycle count landing below what's already reserved is real ground truth, not
        // an error — the correction must still be recordable, not rejected outright. But
        // it also means some order(s) currently counting on this reservation are about to
        // come up short, and a Stock row has no record of which ones (ReservedQuantity is
        // a running total, not an itemized per-order ledger) — this can't be
        // auto-resolved the way a picker's own missing/defect report can, where the
        // specific task/order is right there. Surface it and require a conscious second
        // confirmation instead of either crashing on the DB check constraint or silently
        // shrinking someone's reservation.
        var reservationImpact = Math.Max(0, stock.ReservedQuantity - newPhysicalQuantity);
        if (reservationImpact > 0 && !confirmReservationImpact)
        {
            return Result<StockAdjustmentResultDto>.Failure(
                $"This adjustment would take physical quantity to {newPhysicalQuantity}, below the {stock.ReservedQuantity} " +
                $"unit(s) already reserved here for allocated orders — {reservationImpact} unit(s) of reservation would be " +
                "lost, and the affected order(s) aren't tracked per stock row, so they can't be re-shortaged automatically. " +
                "Investigate which order(s) this affects, then resubmit with confirmation if the correction should proceed.",
                ResultErrorType.Conflict);
        }

        stock.PhysicalQuantity = newPhysicalQuantity;
        if (reservationImpact > 0)
        {
            stock.ReservedQuantity -= reservationImpact;
        }

        _unitOfWork.StockTransactions.Add(new StockTransaction
        {
            ProductId = productId,
            LocationId = location.Id,
            QuantityChange = quantityDelta,
            TransactionType = StockTransactionType.ManualAdjustment,
            UserId = userId
        });

        await _unitOfWork.SaveChangesAsync();

        return Result<StockAdjustmentResultDto>.Success(new StockAdjustmentResultDto
        {
            ProductId = productId,
            LocationBarcode = locationBarcode,
            QuantityDelta = quantityDelta,
            NewPhysicalQuantity = stock.PhysicalQuantity,
            Reason = reason,
            ReservedQuantityReduced = reservationImpact
        });
    }

    public async Task<Result<ProductResponseDto>> CreateProductWithLocationAsync(CreateProductWithLocationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Sku))
            return Result<ProductResponseDto>.Failure("Name and SKU are required.");

        if (string.IsNullOrWhiteSpace(dto.LocationBarcode) || string.IsNullOrWhiteSpace(dto.Sector) || string.IsNullOrWhiteSpace(dto.WarehouseCode))
            return Result<ProductResponseDto>.Failure("Location barcode, sector, and warehouse code are required.");

        if (dto.InitialQuantity < 0)
            return Result<ProductResponseDto>.Failure("Initial quantity cannot be negative.");

        var skuExists = await _unitOfWork.Products.SkuExistsAsync(dto.Sku);
        if (skuExists)
            return Result<ProductResponseDto>.Failure($"A product with SKU '{dto.Sku}' already exists.", ResultErrorType.Conflict);

        // Read-only + a possible early failure, nothing persisted yet — no transaction
        // needed until we're actually about to mutate anything below.
        var location = await _unitOfWork.Locations.GetByBarcodeAsync(dto.LocationBarcode);

        if (location != null)
        {
            // Reuse the existing bin, but its real address must match what the admin entered
            if (location.Sector != dto.Sector || location.WarehouseCode != dto.WarehouseCode || location.Floor != dto.Floor)
            {
                return Result<ProductResponseDto>.Failure(
                    $"Location '{dto.LocationBarcode}' already exists with sector '{location.Sector}', warehouse '{location.WarehouseCode}', floor {location.Floor} — that does not match what was entered.",
                    ResultErrorType.Conflict);
            }
        }
        else
        {
            location = new Location
            {
                AddressBarcode = dto.LocationBarcode,
                Sector = dto.Sector,
                WarehouseCode = dto.WarehouseCode,
                Floor = dto.Floor,
                Type = LocationType.Shelf
            };
            _unitOfWork.Locations.Add(location);
        }

        var (product, stock) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var product = new Product
            {
                Name = dto.Name,
                Sku = dto.Sku,
                Price = dto.Price,
                WeightKg = dto.WeightKg,
                LengthCm = dto.LengthCm,
                WidthCm = dto.WidthCm,
                HeightCm = dto.HeightCm,
                BaseUnit = (UnitType)dto.BaseUnit,
                ItemPerPackage = dto.ItemPerPackage
            };
            _unitOfWork.Products.Add(product);

            var stock = new Stock
            {
                Product = product,
                Location = location,
                PhysicalQuantity = dto.InitialQuantity,
                ReservedQuantity = 0
            };
            _unitOfWork.Stocks.Add(stock);

            await _unitOfWork.SaveChangesAsync();

            return (product, stock);
        });

        return Result<ProductResponseDto>.Success(new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            SizeCategory = product.SizeCategory.ToString(),
            Stocks = new List<StockCreateDto>
            {
                new()
                {
                    ProductId = product.Id,
                    LocationBarcode = location.AddressBarcode,
                    Quantity = stock.PhysicalQuantity
                }
            }
        });
    }
}
