using Warehouse.Application.DTOs;
using Warehouse.Domain;

namespace Warehouse.Application.Mapping;

public static class PickTaskMappingExtensions
{
    // Pure shaping only — no route optimization, no I/O. Callers that need the
    // items in walking order apply that as an explicit separate step.
    public static PickTaskResponseDto ToDto(this PickTask task)
    {
        return new PickTaskResponseDto
        {
            Id = task.Id,
            Sector = task.Sector,
            Status = task.Status.ToString(),
            AssignedWorkerId = task.AssignedWorkerId,
            // The client shows this barcode as the container to scan on completion
            ContainerBarcode = task.Container?.Barcode,
            Items = task.Items.Select(i => new PickTaskItemResponseDto
            {
                Id = i.Id,
                ProductName = i.Product!.Name,
                ProductSku = i.Product.Sku,
                LocationBarcode = i.Location!.AddressBarcode,
                RequiredQuantity = i.RequiredQuantity,
                PickedQuantity = i.PickedQuantity,
                MissingQuantity = i.MissingQuantity,
                AvailableStock = i.Location.Stocks
                    .FirstOrDefault(s => s.ProductId == i.ProductId)?.AvailableQuantity ?? 0
            }).ToList()
        };
    }
}
