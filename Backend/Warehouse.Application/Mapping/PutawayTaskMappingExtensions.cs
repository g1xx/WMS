using Warehouse.Application.DTOs;
using Warehouse.Domain;

namespace Warehouse.Application.Mapping;

public static class PutawayTaskMappingExtensions
{
    // Pure shaping only — no route optimization. Suggestions are passed in already
    // fetched, since resolving them is an async repository call the caller owns.
    public static PutawayTaskResponseDto ToDto(this PutawayTask task, IReadOnlyDictionary<Guid, List<string>> suggestedLocationsByProduct)
    {
        return new PutawayTaskResponseDto
        {
            Id = task.Id,
            ContainerBarcode = task.Container?.Barcode ?? string.Empty,
            Sector = task.Sector,
            Status = task.Status.ToString(),
            Items = task.Items.Select(i => new PutawayTaskItemResponseDto
            {
                Id = i.Id,
                ProductName = i.Product!.Name,
                ProductSku = i.Product.Sku,
                ExpectedQuantity = i.ExpectedQuantity,
                PutAwayQuantity = i.PutAwayQuantity,
                MissingQuantity = i.MissingQuantity,
                SuggestedLocationBarcodes = suggestedLocationsByProduct.TryGetValue(i.ProductId, out var barcodes)
                    ? barcodes
                    : new List<string>()
            }).ToList()
        };
    }
}
