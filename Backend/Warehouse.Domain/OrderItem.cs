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

    // Units permanently written off this line (defective or genuinely missing) that
    // no replacement pick from an active picking zone could cover. RequiredQuantity
    // is never mutated for this — it always reflects what was actually ordered, so
    // ShortedQuantity is the one place a short-shipment is recorded. Dispatch treats
    // PickedQuantity + ShortedQuantity >= RequiredQuantity as "line resolved".
    public int ShortedQuantity { get; set; } = 0;

    // Set once any unit on this line could only be covered by stock outside the
    // active picking zones (bulk/reserve storage) or nowhere at all.
    public bool IsPendingReplenishment { get; set; } = false;
}