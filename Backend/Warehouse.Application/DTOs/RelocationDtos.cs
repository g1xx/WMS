namespace Warehouse.Application.DTOs;

// One product sitting at a location — used both for "what's on this shelf" (source scan)
// and "what am I carrying" (putaway leg).
public class RelocationStockLineDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int PhysicalQuantity { get; set; }

    // Reserved for a pick task. Relocatable quantity is Physical - Reserved: moving
    // reserved units would send a picker to an empty slot.
    public int ReservedQuantity { get; set; }

    // What the quantity input defaults to, and the most that may be taken. Always
    // PhysicalQuantity - ReservedQuantity at a real location; on the transit location
    // nothing is ever reserved, so it equals PhysicalQuantity.
    public int AvailableQuantity { get; set; }
}

// Everything the relocation screen needs to render: what the worker is carrying, and
// whether they're allowed to leave.
public class RelocationStateDto
{
    public string TransitBarcode { get; set; } = string.Empty;

    public List<RelocationStockLineDto> CarriedItems { get; set; } = new();

    // False whenever anything is still carried. A worker must not walk away holding
    // stock — see RelocationService.GetStateAsync for the caveat this does NOT cover.
    public bool CanExit { get; set; }
}

public class LocationContentsDto
{
    public string LocationBarcode { get; set; } = string.Empty;
    public List<RelocationStockLineDto> Items { get; set; } = new();
}

// Source leg: shelf -> the worker's hands.
public class RelocationTakeDto
{
    public string SourceLocationBarcode { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

// Destination leg: the worker's hands -> shelf. Partial quantities are expected — a
// carried SKU may be split across several target locations.
public class RelocationPutawayDto
{
    public string TargetLocationBarcode { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
