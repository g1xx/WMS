using Warehouse.Application.Common;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Services;

// Backs the read-only "Informacja o..." lookup screen. Every method reads; none mutates.
public interface IInfoService
{
    // By SKU only. Product has no barcode column — Sku is the scanned identifier
    // everywhere in this system (PickItemDto.ProductSku, ConfirmPutawayItemDto.ProductSku),
    // so there is no separate barcode to look up by.
    Task<Result<ProductInfoDto>> GetProductInfoAsync(string sku);

    Task<Result<ContainerInfoDto>> GetContainerInfoAsync(string barcode);

    Task<Result<LocationInfoDto>> GetLocationInfoAsync(string barcode);
}
