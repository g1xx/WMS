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
}