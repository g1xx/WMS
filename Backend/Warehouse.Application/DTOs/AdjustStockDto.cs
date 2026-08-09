namespace Warehouse.Application.DTOs;

public class AdjustStockDto
{
    public Guid ProductId { get; set; }

    // Stock is location-scoped, so the specific shelf being corrected must be named
    public string LocationBarcode { get; set; } = string.Empty;

    // Positive to add, negative to remove
    public int QuantityDelta { get; set; }

    public string Reason { get; set; } = string.Empty;
}
