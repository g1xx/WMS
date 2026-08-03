namespace Warehouse.Domain;

public enum OrderStatus
{
    New,        // Landed in the system, nobody has touched it yet
    Picking,    // A warehouse worker is picking it right now
    Packed,     // Picked, sitting on the ramp
    Shipped,    // Left for the customer
    Canceled,   // Canceled

    // Appended last on purpose: the enum is persisted as int, so inserting
    // a value above would shift the stored values of the existing statuses.
    AwaitingReplenishment // Parked: not enough stock to allocate the order
}

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}