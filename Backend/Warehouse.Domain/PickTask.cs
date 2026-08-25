namespace Warehouse.Domain;

public enum PickTaskStatus
{
    New,
    InProgress, 
    Completed,  
    Canceled    
}

public class PickTask
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public string Sector { get; set; } = string.Empty;

    public PickTaskStatus Status { get; set; } = PickTaskStatus.New;

    public string? AssignedWorkerId { get; set; }

    // When this task was claimed for a worker — set together with AssignedWorkerId the
    // moment the task is SHOWN, so two workers can't be offered the same one. Only
    // meaningful while Status is New: that combination means "handed to a worker who
    // hasn't scanned a container yet," and it's the only state the inactivity sweep can
    // release. Scanning a container flips Status to InProgress (StartPickTaskAsync),
    // which puts the task permanently out of the sweep's reach — a picker may spend a
    // long time at the racks and must never have the task taken away mid-pick.
    // Cleared alongside AssignedWorkerId on release.
    public DateTime? ClaimedAt { get; set; }

    public Guid? ContainerId { get; set; }
    public Container? Container { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PickTaskItem> Items { get; set; } = new List<PickTaskItem>();
}