namespace Warehouse.Application.DTOs;

// Read-only lookup screen ("Informacja o..."). Nothing here mutates anything.

public class ProductLocationLineDto
{
    public string LocationBarcode { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;

    public int PhysicalQuantity { get; set; }
    public int ReservedQuantity { get; set; }

    // Physical minus reserved — what could actually be picked or relocated from here.
    public int AvailableQuantity { get; set; }
}

public class ProductInfoDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public string SizeCategory { get; set; } = string.Empty;

    // Includes locations sitting at zero: a zero row is a slot this SKU has been stored in
    // before — its home slot, currently empty — which is worth seeing. Excludes Transit;
    // see CarriedByWorkersQuantity.
    public List<ProductLocationLineDto> Locations { get; set; } = new();

    // Units currently in workers' hands, summed across every transit location. Kept OUT of
    // the list above and surfaced as one number on purpose: the list answers "where do I
    // walk to find this", and a transit location is not somewhere anyone can walk to — its
    // barcode isn't a real address. Hiding it entirely would be worse though, because then
    // physical stock would silently vanish from the one screen built to answer "where is
    // this SKU", so the total stays reconcilable.
    public int CarriedByWorkersQuantity { get; set; }
}

public class ContainerLinkedTaskDto
{
    // "Picking" or "Putaway" — which flow currently holds this container.
    public string Kind { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
}

public class ContainerInfoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Null when the container isn't recorded at any location.
    public string? LocationBarcode { get; set; }

    // The picking or putaway zone it's committed to, if any.
    public string? AssignedSector { get; set; }

    // Null when nothing currently holds it.
    public ContainerLinkedTaskDto? LinkedTask { get; set; }

    // Always false for now. Container contents are not modelled as Stock (see the comment
    // on Container.Stocks — that navigation is always empty), so answering "what's inside"
    // means deriving it from task lines, which is deferred. The flag exists so the client
    // can say "not available yet" rather than render an empty list that reads as "empty".
    public bool ContentsAvailable { get; set; }
}

public class LocationStockLineDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int PhysicalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class LocationInfoDto
{
    public string Barcode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;

    public List<LocationStockLineDto> Items { get; set; } = new();

    // SKUs physically present (a row at zero occupies no slot), against the effective
    // limit — the per-row override if set, otherwise the LocationType default. Null means
    // no limit, never zero.
    public int DistinctSkuCount { get; set; }
    public int? MaxDistinctSkus { get; set; }
}
