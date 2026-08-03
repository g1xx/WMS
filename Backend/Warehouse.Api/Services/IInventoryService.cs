using Warehouse.Api.Common;
using Warehouse.Api.DTOs;

namespace Warehouse.Api.Services;

public interface IInventoryService
{
    Task<Result<StockAdjustmentResultDto>> AdjustPhysicalStockAsync(Guid productId, string locationBarcode, int quantityDelta, string reason);

    Task<Result<ProductResponseDto>> CreateProductWithLocationAsync(CreateProductWithLocationDto dto);
}
