namespace Warehouse.Application.DTOs;

public class StockAdjustmentResultDto
{
    public Guid ProductId { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;
    public int QuantityDelta { get; set; }
    public int NewPhysicalQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;

    // > 0 when this adjustment had to shrink ReservedQuantity to keep it from
    // exceeding the new physical count — some allocated order(s) at this location are
    // now short by this many units and need manual investigation (see
    // AdjustStockDto.ConfirmReservationImpact).
    public int ReservedQuantityReduced { get; set; }
}
