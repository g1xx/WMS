namespace Warehouse.Domain;

public class PutawayTaskItem
{
    public Guid Id { get; set; }

    public Guid PutawayTaskId { get; set; }
    public PutawayTask? PutawayTask { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int ExpectedQuantity { get; set; }
    public int PutAwayQuantity { get; set; } = 0;
    public int MissingQuantity { get; set; } = 0;
}
