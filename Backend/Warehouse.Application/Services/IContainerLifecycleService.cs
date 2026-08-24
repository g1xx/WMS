using Warehouse.Application.Common;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public interface IContainerLifecycleService
{
    // The only place Container.Status is ever assigned after creation. Locks the
    // container row (SELECT ... FOR UPDATE) and re-reads its true current status
    // bypassing EF's change tracker, then only mutates it if that fresh read still
    // matches `from`. Does not call SaveChangesAsync — the caller bundles this into
    // its own transaction alongside whatever else the same operation needs to persist
    // (e.g. assigning a PickTask to the container), same composition pattern already
    // used for the MaxDistinctSkus putaway check.
    //
    // (from, to) not found in ContainerTransitions throws InvalidOperationException —
    // that's a code bug (a caller invoking a move that was never supposed to exist),
    // not a runtime business outcome, so it doesn't get a Result.
    Task<Result<Container>> TransitionAsync(Guid containerId, ContainerStatus from, ContainerStatus to);
}
