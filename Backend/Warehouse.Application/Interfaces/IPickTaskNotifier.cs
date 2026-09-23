namespace Warehouse.Application.Interfaces;

// Lets the pick-task workflow (allocation, cancel, dispatch leftovers, the
// missing/defect replacement handler) push a "something changed" signal to
// whichever transport the API layer wires up — SignalR today — without the
// Application layer depending on it directly. A no-op implementation is a
// valid choice too (e.g. for a test double), which is why this only signals
// that a sector's task list changed rather than shipping the task itself:
// callers already know how to refetch it.
public interface IPickTaskNotifier
{
    Task NotifySectorChangedAsync(string sector);
}
