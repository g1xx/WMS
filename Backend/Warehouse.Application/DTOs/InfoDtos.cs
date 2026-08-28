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

public class ContainerContentLineDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

// Container contents are DERIVED, not stored — Stock has no ContainerId (see the comment
// on Container.Stocks), so everything here is reconstructed from task lines. Different
// reconstructions carry very different confidence, so each is reported as its own section
// with its own provenance rather than merged into one number.
//
// In particular a container that was picked into, dispatched, and is now partly put away
// yields TWO sections — what went in at dispatch, and what is still to come out. They are
// never subtracted from each other: PutawayTaskItem.ExpectedQuantity is supplied by
// whoever created the task, not derived from what was picked, so the two figures are not
// guaranteed to describe the same physical units. Subtracting them would manufacture a
// number that looks authoritative and can be arbitrarily wrong.
public class ContainerContentSectionDto
{
    // Empty | BeingPickedInto | ToBePutAway | AsDispatched | Unknown
    public string Kind { get; set; } = string.Empty;

    public List<ContainerContentLineDto> Lines { get; set; } = new();

    // Which task this section was reconstructed from, where there is one.
    public Guid? SourceTaskId { get; set; }
    public string? Sector { get; set; }

    // True only for AsDispatched. That section is a statement about the PAST — these units
    // were picked into this container — and not a claim about what is inside it now.
    // Nothing invalidates it: only a completing putaway returns the container to Available,
    // so if it was emptied any other way (tipped out, shipped off the conveyor, corrected
    // by a cycle count) this keeps reporting the same lines indefinitely. Clients must
    // render it as history, visibly distinct from the live sections.
    public bool IsHistorical { get; set; }
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

    // Every task currently holding this container, not just one. A container can have
    // several pending putaway tasks at once — PutawayTask documents one per zone when its
    // expected items span multiple zones — so reporting a single "the" task would pick
    // arbitrarily among them and present the choice as fact.
    public List<ContainerLinkedTaskDto> LinkedTasks { get; set; } = new();

    // One or more independently-sourced views of what's inside. See
    // ContainerContentSectionDto for why this is a list rather than one answer.
    public List<ContainerContentSectionDto> ContentSections { get; set; } = new();
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
