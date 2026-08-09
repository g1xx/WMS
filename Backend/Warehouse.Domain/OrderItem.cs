namespace Warehouse.Domain;

public class OrderItem
{
    public Guid Id { get; set; }
    // Order
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    // Product
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    // Qty
    public int RequiredQuantity { get; set; }

    public int PickedQuantity { get; set; } = 0;

    // Set when a defect write-off could only be covered by bulk/high-rack stock
    // (never picked directly) and no standard-zone replacement was found.
    public bool IsPendingReplenishment { get; set; } = false;
}