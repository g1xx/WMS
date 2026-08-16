using Warehouse.Application.Common;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Services;

public interface IInventoryService
{
    Task<Result<StockAdjustmentResultDto>> AdjustPhysicalStockAsync(Guid productId, string locationBarcode, int quantityDelta, string reason, bool confirmReservationImpact, string userId);

    Task<Result<ProductResponseDto>> CreateProductWithLocationAsync(CreateProductWithLocationDto dto);
}
