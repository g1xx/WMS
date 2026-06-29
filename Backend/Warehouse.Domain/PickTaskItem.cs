namespace Warehouse.Domain;

public class PickTaskItem
{
    public Guid Id { get; set; }

    public Guid PickTaskId { get; set; }
    public PickTask? PickTask { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public int RequiredQuantity { get; set; }
    public int PickedQuantity { get; set; } = 0;
}