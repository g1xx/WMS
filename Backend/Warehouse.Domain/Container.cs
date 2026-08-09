namespace Warehouse.Domain;

public enum ContainerType
{
    Tote,      // Picking tote
    Palox,     // Palox
    Pallet     // Pallet
}

public enum ContainerStatus
{
    // Persisted as int (no HasConversion) — renaming a member is safe,
    // but never reorder these or existing rows will be reinterpreted.
    New,        // Empty and available to be picked up for a task
    InProgress, // Assigned to an active pick or putaway task
    Ready,      // Picked and staged for the conveyor
    Available   // Emptied after task completion and released back into the free pool
}
public class Container
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public ContainerType Type { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.New;

    public decimal MaxWeightCapacityKg { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    // The picking/putaway zone this container is currently committed to, if any.
    // Cleared when the container is released back to the free pool.
    public string? AssignedSector { get; set; }
}