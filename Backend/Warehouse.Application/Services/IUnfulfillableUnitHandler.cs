using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class UnfulfillableUnitResult
{
    // Replacement units appended to the worker's current PickTask (same zone).
    public int AppendedToCurrentTaskQuantity { get; set; }

    // One entry per new PickTask created in a different active picking zone.
    public List<Guid> NewPickTaskIds { get; set; } = new();

    // Units that could not be sourced from any active picking zone (only bulk/reserve
    // stock left, or none at all) — written off against the order as ShortedQuantity.
    public int ShortageQuantity { get; set; }
}

// Shared by ReportDefectAsync and ReportMissingItemAsync for the moment either one
// determines a unit can no longer come from where it was originally reserved —
// defective or genuinely missing. Both are the same problem once the reason-specific
// stock/StockTransaction adjustment is done: find a replacement pick somewhere
// reachable, or write the shortfall off against the order. Callers own the
// reason-specific inventory side effects; this owns the replacement search and the
// order-level consequence, so the two report paths cannot drift apart on it again.
public interface IUnfulfillableUnitHandler
{
    Task<UnfulfillableUnitResult> HandleAsync(PickTask task, Guid productId, Guid excludeLocationId, int quantityNeeded);
}
