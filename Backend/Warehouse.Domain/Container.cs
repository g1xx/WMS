namespace Warehouse.Domain;

public enum ContainerType
{
    Tote,      // Picking tote
    Palox,     // Palox
    Pallet     // Pallet
}

public enum ContainerStatus
{
    // Persisted as int (no HasConversion) — renaming a member is safe, but never
    // reorder these or existing rows will be reinterpreted. Explicit values, not
    // sequential defaults: New (0) was removed (see the AddContainerAvailableStatus
    // migration, which maps every existing New row to Available) and letting C#
    // auto-renumber the rest would have silently shifted InProgress/Ready/Available
    // down into 0/1/2, reinterpreting every existing row.
    InProgress = 1, // Someone is actively working with it (picking or putaway)
    Ready = 2,      // Loaded — staged on the conveyor (picking) or arrived from
                    // receiving, not yet started (putaway). Still physically full;
                    // NOT free, unlike the bug this status exists to prevent.
    Available = 3   // Free — the only status a container can be claimed from.
}
public class Container
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public ContainerType Type { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.Available;

    public decimal MaxWeightCapacityKg { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    // ALWAYS EMPTY — do not use this to answer "what is in this container".
    //
    // Stock is keyed by (ProductId, LocationId) and has no ContainerId of its own. This
    // navigation has no configured relationship, so EF Core inferred one by convention and
    // created a shadow nullable ContainerId column (plus an index) on Stocks. Nothing in
    // the codebase ever writes it, so the column is uniformly NULL and this collection
    // always materialises as zero rows — silently, with no error to notice.
    //
    // Container contents have to be derived from tasks instead: picked lines on the
    // PickTask holding the container, or outstanding lines on its PutawayTasks.
    //
    // Dropping the shadow column is queued as its own change (a migration with its own
    // risk profile), which is why this stays here for now rather than being deleted.
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    // The picking/putaway zone this container is currently committed to, if any.
    // Cleared when the container is released back to the free pool.
    public string? AssignedSector { get; set; }
}