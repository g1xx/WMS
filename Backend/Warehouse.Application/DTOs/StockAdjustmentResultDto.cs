namespace Warehouse.Application.DTOs;

public class StockAdjustmentResultDto
{
    public Guid ProductId { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;
    public int QuantityDelta { get; set; }
    public int NewPhysicalQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
