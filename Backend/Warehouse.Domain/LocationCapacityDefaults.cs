namespace Warehouse.Domain;

// Per-LocationType fallback used when Location.MaxDistinctSkus is null.
//   - Shelf and FloorZone are both real storage, not staging — FloorZone (bulk/reserve
//     storage) gets the same default as Shelf.
//   - DockDoor/ConveyorDrop/Ramp are staging/transit, never real putaway destinations —
//     null (no limit), not zero.
public static class LocationCapacityDefaults
{
    public static int? GetDefaultMaxDistinctSkus(LocationType type) => type switch
    {
        LocationType.Shelf => 3,
        LocationType.FloorZone => 3,
        _ => null,
    };
}
