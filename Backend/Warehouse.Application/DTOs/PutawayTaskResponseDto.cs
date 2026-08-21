namespace Warehouse.Application.DTOs;

// Ranked candidate destination for a putaway suggestion — a reference for the worker,
// not a restriction (they can still scan anywhere; see PutawayService.ConfirmItemAsync).
// The list this appears in (PutawayTaskItemResponseDto.SuggestedLocations) is
// pre-sorted server-side: locations in the worker's current sector that already stock
// this SKU, then same-sector locations that held it before and are now empty (its
// "home slots" — CurrentQuantity == 0 doesn't mean drop it from the list), then
// other-sector locations that currently stock it, informational only.
public class SuggestedPutawayLocationDto
{
    public string LocationBarcode { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public bool IsInCurrentSector { get; set; }
    public int DistinctSkuCount { get; set; }
    // Resolved (Location.MaxDistinctSkus ?? LocationCapacityDefaults for its Type);
    // null means unlimited. Locations at/over this are excluded already, except where
    // CurrentQuantity > 0 exempts them (see PutawayService) — shown here so the worker
    // can see how much room is left, not just that there is some.
    public int? MaxDistinctSkus { get; set; }
}

public class PutawayTaskItemResponseDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int PutAwayQuantity { get; set; }
    public int MissingQuantity { get; set; }

    public List<SuggestedPutawayLocationDto> SuggestedLocations { get; set; } = new();
}

public class PutawayTaskResponseDto
{
    public Guid Id { get; set; }
    public string ContainerBarcode { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PutawayTaskItemResponseDto> Items { get; set; } = new();
}
