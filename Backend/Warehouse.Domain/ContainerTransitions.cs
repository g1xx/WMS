namespace Warehouse.Domain;

// A container is a shared physical resource — its status IS the lock. This table is the
// only place the allowed moves are defined; ContainerLifecycleService (Application layer)
// is the only place allowed to act on it. Nothing else should assign Container.Status
// after creation.
public static class ContainerTransitions
{
    // The single definition of "free" — the only status a container can be claimed
    // from. ContainerRepository's read-filters reference this directly, so the
    // repository's idea of "free" and this table's claimable state can't drift apart
    // the way New/Available's independent duplication did before.
    public const ContainerStatus FreeStatus = ContainerStatus.Available;

    private static readonly HashSet<(ContainerStatus From, ContainerStatus To)> Allowed = new()
    {
        (FreeStatus, ContainerStatus.InProgress),              // picking claims it
        (ContainerStatus.InProgress, ContainerStatus.Ready),         // picking stages it on the conveyor — still physically loaded, not free
        (ContainerStatus.InProgress, FreeStatus),              // picking-cancel before anything picked, AND putaway finishing — same pair, two callers
        (ContainerStatus.Ready, ContainerStatus.InProgress),         // putaway starts on a container that arrived staged
    };

    public static bool IsAllowed(ContainerStatus from, ContainerStatus to) => Allowed.Contains((from, to));
}
