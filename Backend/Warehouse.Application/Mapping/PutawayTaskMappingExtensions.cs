using Warehouse.Application.DTOs;
using Warehouse.Domain;

namespace Warehouse.Application.Mapping;

public static class PutawayTaskMappingExtensions
{
    // Pure shaping only — no ranking/filtering logic and no route optimization here.
    // Suggestions are passed in already fetched and already ranked (see
    // PutawayService.MapToDtoWithSuggestionsAsync), since both resolving them and
    // deciding their order need task.Sector plus async repository calls the caller owns.
    public static PutawayTaskResponseDto ToDto(this PutawayTask task, IReadOnlyDictionary<Guid, List<SuggestedPutawayLocationDto>> suggestedLocationsByProduct)
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
                SuggestedLocations = suggestedLocationsByProduct.TryGetValue(i.ProductId, out var suggestions)
                    ? suggestions
                    : new List<SuggestedPutawayLocationDto>()
            }).ToList()
        };
    }
}
