namespace Warehouse.Domain;

public enum StockTransactionType
{
    Pick,
    Putaway,
    Defect,
    Missing,
    ManualAdjustment,

    // Both legs of a stock relocation — source -> transit, then transit -> target. One
    // type rather than two: the legs are already distinguishable by the sign of
    // QuantityChange and by which of the two locations the row names, and calling them
    // Pick/Putaway would pollute those counts with movements that never touched an order.
    //
    // Appended, never inserted — persisted as int, same reasoning as LocationType.Transit.
    Relocation
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
