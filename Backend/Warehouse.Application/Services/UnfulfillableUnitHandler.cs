using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class UnfulfillableUnitHandler : IUnfulfillableUnitHandler
{
    // Bulk/high-rack storage sector (e.g. the "w" in "mw1") — never a valid source
    // for a picker to be routed to for a replacement pick.
    private const string BulkSector = "w";

    // The only zones a picker can physically be sent to for a replacement pick —
    // bulk/reserve storage requires a forklift and is too slow for a picker to
    // detour into mid-task. Any stock outside this set is unreachable for this
    // purpose no matter how much of it exists, and must be treated the same as
    // no stock at all: an unrecoverable shortfall.
    private static readonly HashSet<string> ActivePickingZones = new()
    {
        "mp1", "mr1", "mg1",
        "mp2", "mr2", "mg2",
        "mp3", "mr3",
        "mp4", "mr4", "mg4",
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDefectReplacementPlanner _replacementPlanner;

    public UnfulfillableUnitHandler(IUnitOfWork unitOfWork, IDefectReplacementPlanner replacementPlanner)
    {
        _unitOfWork = unitOfWork;
        _replacementPlanner = replacementPlanner;
    }

    public async Task<UnfulfillableUnitResult> HandleAsync(PickTask task, Guid productId, Guid excludeLocationId, int quantityNeeded)
    {
        var result = new UnfulfillableUnitResult();

        if (quantityNeeded <= 0)
            return result;

        var candidateStocks = await _unitOfWork.Stocks.GetReplacementCandidatesAsync(productId, excludeLocationId, BulkSector);

        // GetReplacementCandidatesAsync only excludes the single bulk sector at the
        // query level — the active-zone allow-list is the authoritative filter and is
        // enforced here, so nothing outside it (bulk or any other non-active zone) is
        // ever handed to a picker as a replacement.
        var reachableStocks = candidateStocks.Where(s => ActivePickingZones.Contains(s.Location!.ZoneCode)).ToList();

        var plan = _replacementPlanner.Plan(reachableStocks, quantityNeeded);

        foreach (var pick in plan.Picks)
        {
            pick.Stock.ReservedQuantity += pick.Quantity;
        }

        // Same zone as the current task: append a new line to it.
        foreach (var pick in plan.Picks.Where(p => p.ZoneCode == task.Sector))
        {
            task.Items.Add(new PickTaskItem
            {
                PickTaskId = task.Id,
                ProductId = productId,
                LocationId = pick.Stock.LocationId,
                RequiredQuantity = pick.Quantity,
                PickedQuantity = 0
            });
            result.AppendedToCurrentTaskQuantity += pick.Quantity;
        }

        // Different active zone: a brand new PickTask targeted at that zone.
        foreach (var zoneGroup in plan.Picks.Where(p => p.ZoneCode != task.Sector).GroupBy(p => p.ZoneCode))
        {
            var newTask = new PickTask
            {
                OrderId = task.OrderId,
                Sector = zoneGroup.Key,
                Status = PickTaskStatus.New,
                Items = zoneGroup.Select(p => new PickTaskItem
                {
                    ProductId = productId,
                    LocationId = p.Stock.LocationId,
                    RequiredQuantity = p.Quantity,
                    PickedQuantity = 0
                }).ToList()
            };

            _unitOfWork.PickTasks.Add(newTask);
            result.NewPickTaskIds.Add(newTask.Id);
        }

        // Whatever is still short only exists outside active picking zones (or
        // nowhere) — never hand it to a picker. Write it off against the order instead.
        if (plan.ShortageQuantity > 0)
        {
            result.ShortageQuantity = plan.ShortageQuantity;

            var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(task.OrderId);
            var orderItem = order?.Items.FirstOrDefault(oi => oi.ProductId == productId);
            if (orderItem != null)
            {
                orderItem.ShortedQuantity += plan.ShortageQuantity;
                orderItem.IsPendingReplenishment = true;
            }
        }

        return result;
    }
}
