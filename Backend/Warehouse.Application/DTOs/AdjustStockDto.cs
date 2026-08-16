namespace Warehouse.Application.DTOs;

public class AdjustStockDto
{
    public Guid ProductId { get; set; }

    // Stock is location-scoped, so the specific shelf being corrected must be named
    public string LocationBarcode { get; set; } = string.Empty;

    // Positive to add, negative to remove
    public int QuantityDelta { get; set; }

    public string Reason { get; set; } = string.Empty;

    // Required (true) whenever the adjustment would take physical quantity below the
    // stock's current ReservedQuantity — that erases reservation capacity some
    // allocated order is counting on. Stock rows don't track which order(s) reserved
    // them, so this can't be auto-resolved; the caller must have seen the warning and
    // explicitly accepted it before it's applied. Ignored when there's no such impact.
    public bool ConfirmReservationImpact { get; set; } = false;
}
