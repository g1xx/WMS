using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IPickTaskRepository
{
    // Container, Items.Product, Items.Location.Stocks — matches the GetPickTasksAsync,
    // GetActiveTaskForUserAsync and GetNextTaskAsync projection shapes.
    Task<List<PickTask>> GetAllWithDetailsAsync();
    // The task this worker HOLDS — either started (InProgress) or claimed at show-time but
    // not yet begun (New + assigned). Both count: GetNextForSectorAsync skips every task
    // that has an assignee, including this worker's own, so this is the only query that can
    // hand a worker back the task they were already given.
    Task<PickTask?> GetActiveForUserAsync(string userId);
    Task<PickTask?> GetNextForSectorAsync(string sector);

    // Atomically hands the oldest unclaimed New task in the sector to workerId, setting
    // AssignedWorkerId + ClaimedAt while leaving Status = New. Returns null when the
    // sector has no claimable work. MUST be called inside a transaction: the row lock it
    // takes is what makes the claim atomic, and it's only held until that transaction
    // commits. See the implementation for why this uses SKIP LOCKED rather than blocking
    // the way the container claim does.
    Task<PickTask?> ClaimNextForSectorAsync(string sector, string workerId, DateTime claimedAt);

    // Un-claims New tasks in the sector that were shown to a worker before `cutoff` and
    // never progressed to a container scan. Returns how many were released. Cannot touch
    // an InProgress task by construction. Call inside the same transaction as the claim
    // above, before it, so freed tasks are immediately claimable.
    Task<int> ReleaseExpiredClaimsAsync(string sector, DateTime cutoff);

    // Releases one specific task, but only if it's still New and still claimed by this
    // worker — a task the worker has since started (or that the sweep already released
    // and handed to someone else) is left untouched. Returns whether anything was released.
    Task<bool> ReleaseClaimAsync(Guid taskId, string workerId);

    Task<PickTask?> GetByIdAsync(Guid id);

    // Items.Product, Items.Location — used by PickItemAsync, ReportMissingItemAsync, ReportDefectAsync.
    Task<PickTask?> GetByIdWithItemsAndProductLocationAsync(Guid id);

    // Items only — used by DispatchContainerAsync, CancelPickTaskAsync.
    Task<PickTask?> GetByIdWithItemsAsync(Guid id);

    // Container.Location included — feeds OrdersController.PackOrder's readiness check.
    Task<List<PickTask>> GetByOrderIdWithContainerLocationAsync(Guid orderId);

    void Add(PickTask task);
}
