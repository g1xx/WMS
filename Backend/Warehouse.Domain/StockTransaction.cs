namespace Warehouse.Domain;

public enum StockTransactionType
{
    Pick,
    Putaway,
    Defect,
    Missing,
    ManualAdjustment
}

// Immutable audit trail entry for every change to Stock.PhysicalQuantity.
public class StockTransaction
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    // Positive for additions (putaway, manual adjustment up), negative for removals.
    public int QuantityChange { get; set; }

    public StockTransactionType TransactionType { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
