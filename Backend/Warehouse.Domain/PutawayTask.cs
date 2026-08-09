namespace Warehouse.Domain;

public enum PutawayTaskStatus
{
    New,
    InProgress,
    Completed,
    Canceled
}

public class PutawayTask
{
    public Guid Id { get; set; }

    public Guid ContainerId { get; set; }
    public Container? Container { get; set; }

    // Zone code, e.g. "mp1" — same WarehouseCode+Sector+Floor convention as PickTask.Sector.
    // A single physical container can have one PutawayTask per zone if its expected
    // items span multiple zones (mirrors how a multi-zone order becomes multiple PickTasks).
    public string Sector { get; set; } = string.Empty;

    public PutawayTaskStatus Status { get; set; } = PutawayTaskStatus.New;

    public string? AssignedWorkerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PutawayTaskItem> Items { get; set; } = new List<PutawayTaskItem>();
}
