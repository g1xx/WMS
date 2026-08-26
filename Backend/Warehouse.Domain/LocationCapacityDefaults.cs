namespace Warehouse.Domain;

// Per-LocationType fallback used when Location.MaxDistinctSkus is null.
//   - Shelf and FloorZone are both real storage, not staging — FloorZone (bulk/reserve
//     storage) gets the same default as Shelf.
//   - DockDoor/ConveyorDrop/Ramp are staging/transit, never real putaway destinations —
//     null (no limit), not zero.
//   - Transit is what a worker is carrying. Stated explicitly rather than left to the
//     fall-through: a picker's hands are not a storage slot, and capping how many
//     distinct SKUs someone may carry would block a legitimate multi-SKU relocation for
//     no physical reason. The limit still applies normally when they put away into a
//     real location, which is where shelf capacity actually matters.
public static class LocationCapacityDefaults
{
    public static int? GetDefaultMaxDistinctSkus(LocationType type) => type switch
    {
        LocationType.Shelf => 3,
        LocationType.FloorZone => 3,
        LocationType.Transit => null,
        _ => null,
    };
}
