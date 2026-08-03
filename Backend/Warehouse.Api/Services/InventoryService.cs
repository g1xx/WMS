using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Common;
using Warehouse.Api.DTOs;
using Warehouse.Domain;
using Warehouse.Infrastructure;

namespace Warehouse.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StockAdjustmentResultDto>> AdjustPhysicalStockAsync(Guid productId, string locationBarcode, int quantityDelta, string reason)
    {
        if (quantityDelta == 0)
            return Result<StockAdjustmentResultDto>.Failure("Quantity delta must not be zero.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result<StockAdjustmentResultDto>.Failure("A reason is required for a manual stock adjustment.");

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return Result<StockAdjustmentResultDto>.Failure($"Product {productId} was not found.", ResultErrorType.NotFound);

        var location = await _context.Locations.FirstOrDefaultAsync(l => l.AddressBarcode == locationBarcode);
        if (location == null)
            return Result<StockAdjustmentResultDto>.Failure($"Location '{locationBarcode}' was not found.", ResultErrorType.NotFound);

        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == location.Id);

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
            _context.Stocks.Add(stock);
        }

        var newPhysicalQuantity = stock.PhysicalQuantity + quantityDelta;
        if (newPhysicalQuantity < 0)
            return Result<StockAdjustmentResultDto>.Failure(
                $"Adjustment would take physical quantity negative (currently {stock.PhysicalQuantity}, delta {quantityDelta}).");

        stock.PhysicalQuantity = newPhysicalQuantity;

        await _context.SaveChangesAsync();

        return Result<StockAdjustmentResultDto>.Success(new StockAdjustmentResultDto
        {
            ProductId = productId,
            LocationBarcode = locationBarcode,
            QuantityDelta = quantityDelta,
            NewPhysicalQuantity = stock.PhysicalQuantity,
            Reason = reason
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

        var skuExists = await _context.Products.AnyAsync(p => p.Sku == dto.Sku);
        if (skuExists)
            return Result<ProductResponseDto>.Failure($"A product with SKU '{dto.Sku}' already exists.", ResultErrorType.Conflict);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.AddressBarcode == dto.LocationBarcode);

            if (location != null)
            {
                // Reuse the existing bin, but its real address must match what the admin entered
                if (location.Sector != dto.Sector || location.WarehouseCode != dto.WarehouseCode || location.Floor != dto.Floor)
                {
                    await transaction.RollbackAsync();
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
                _context.Locations.Add(location);
            }

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
            _context.Products.Add(product);

            var stock = new Stock
            {
                Product = product,
                Location = location,
                PhysicalQuantity = dto.InitialQuantity,
                ReservedQuantity = 0
            };
            _context.Stocks.Add(stock);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
