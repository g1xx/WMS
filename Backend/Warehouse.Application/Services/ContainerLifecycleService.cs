using Warehouse.Application.Common;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class ContainerLifecycleService : IContainerLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;

    public ContainerLifecycleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Container>> TransitionAsync(Guid containerId, ContainerStatus from, ContainerStatus to)
    {
        if (!ContainerTransitions.IsAllowed(from, to))
            throw new InvalidOperationException($"{from} -> {to} is not an allowed container transition.");

        // Bypasses the change tracker on purpose — this must be the container's true,
        // currently-committed status, not whatever a caller's earlier (pre-lock) read
        // happened to see. Also holds a row lock for the rest of the caller's
        // transaction: a concurrent TransitionAsync on the same container blocks here
        // until this one commits or rolls back, then sees the post-commit status
        // instead of a stale one — this is what actually closes the race, not the
        // check below on its own.
        var currentStatus = await _unitOfWork.Containers.LockForUpdateAsync(containerId);
        if (currentStatus == null)
            return Result<Container>.Failure("Container not found.", ResultErrorType.NotFound);

        if (currentStatus != from)
            return Result<Container>.Failure(
                $"Container is currently {currentStatus}, not {from} — it may have just been taken by someone else.",
                ResultErrorType.Conflict);

        // Now safe to fetch and mutate the tracked entity — nothing else can change
        // this row until our transaction commits or rolls back. GetByIdAsync's
        // FindAsync will return the caller's already-tracked instance if one exists
        // (same DbContext, same request) rather than issuing a redundant query.
        var container = await _unitOfWork.Containers.GetByIdAsync(containerId);
        if (container == null)
            return Result<Container>.Failure("Container not found.", ResultErrorType.NotFound);

        container.Status = to;
        return Result<Container>.Success(container);
    }
}
