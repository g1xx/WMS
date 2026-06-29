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

    public Guid? ContainerId { get; set; }
    public Container? Container { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PickTaskItem> Items { get; set; } = new List<PickTaskItem>();
}