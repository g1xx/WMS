using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class PickTaskRepository : IPickTaskRepository
{
    private readonly AppDbContext _context;

    public PickTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PickTask>> GetAllWithDetailsAsync()
    {
        return await _context.PickTasks
            .AsNoTracking()
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            .ToListAsync();
    }

    public async Task<PickTask?> GetActiveForUserAsync(string userId)
    {
        // "Held by this worker", NOT "started by this worker". Claiming a task at show-time
        // created a second held state — New + assigned — and this query is the ONLY way a
        // worker can be handed back a task they already hold: GetNextForSectorAsync skips
        // anything with an assignee, including their own.
        //
        // Matching InProgress alone made a claimed task invisible to both queries, so the
        // second fetch (React Query refetches on mount and on window focus) showed "no tasks
        // available" and cleared it client-side. That in turn skipped the release-on-exit,
        // which only fires for a task the client still knows about — so the task stayed
        // claimed until the inactivity sweep, i.e. it vanished for 15 minutes.
        return await _context.PickTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            // A worker shouldn't hold both at once, but if they ever do, the started task is
            // the one with physical goods riding on it and must win.
            .OrderByDescending(t => t.Status == PickTaskStatus.InProgress)
            .FirstOrDefaultAsync(t => t.AssignedWorkerId == userId
                                      && (t.Status == PickTaskStatus.InProgress || t.Status == PickTaskStatus.New));
    }

    public async Task<PickTask?> GetNextForSectorAsync(string sector)
    {
        return await _context.PickTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            .FirstOrDefaultAsync(t => t.Status == PickTaskStatus.New && t.AssignedWorkerId == null && t.Sector == sector);
    }

    public async Task<PickTask?> ClaimNextForSectorAsync(string sector, string workerId, DateTime claimedAt)
    {
        // FOR UPDATE SKIP LOCKED, not a plain FOR UPDATE. The container claim
        // (ContainerRepository.LockForUpdateAsync) deliberately blocks, because two workers
        // contending for one SPECIFIC container must serialize and the loser must be told
        // it's taken. Here the workers want ANY task, so blocking B behind A only to hand B
        // a row A just claimed — forcing a retry — is strictly worse than letting B walk
        // straight past it to the next free row. SKIP LOCKED turns the race into a queue.
        //
        // AsNoTracking for the same reason LockForUpdateAsync uses it: this must read the
        // committed row, not a stale instance the change tracker is already holding. The
        // returned id is re-fetched through the tracked path below to be mutated.
        var locked = await _context.Set<PickTask>()
            .FromSqlInterpolated($@"
                SELECT *, xmin FROM ""PickTasks""
                WHERE ""Sector"" = {sector}
                  AND ""Status"" = {(int)PickTaskStatus.New}
                  AND ""AssignedWorkerId"" IS NULL
                ORDER BY ""CreatedAt""
                LIMIT 1
                FOR UPDATE SKIP LOCKED")
            .AsNoTracking()
            .ToListAsync();

        var claimedId = locked.FirstOrDefault()?.Id;
        if (claimedId == null) return null;

        // Safe to mutate now: the row lock above is held until the caller's transaction
        // commits, so nothing else can claim this task in between.
        var task = await _context.PickTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            .FirstOrDefaultAsync(t => t.Id == claimedId);

        if (task == null) return null;

        task.AssignedWorkerId = workerId;
        task.ClaimedAt = claimedAt;

        return task;
    }

    public async Task<int> ReleaseExpiredClaimsAsync(string sector, DateTime cutoff)
    {
        // Status = New in the predicate is what makes "never auto-release a task the worker
        // has actually started" structural rather than a rule someone has to remember: a
        // container scan sets Status = InProgress, and this can't match that row at all.
        return await _context.PickTasks
            .Where(t => t.Sector == sector
                        && t.Status == PickTaskStatus.New
                        && t.AssignedWorkerId != null
                        && t.ClaimedAt != null
                        && t.ClaimedAt < cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.AssignedWorkerId, (string?)null)
                .SetProperty(t => t.ClaimedAt, (DateTime?)null));
    }

    public async Task<bool> ReleaseClaimAsync(Guid taskId, string workerId)
    {
        // Every condition matters: Status = New so a started task is never dropped, and
        // AssignedWorkerId = workerId so a worker whose claim already expired and was
        // re-handed to someone else can't yank it back with a late release call.
        var released = await _context.PickTasks
            .Where(t => t.Id == taskId
                        && t.Status == PickTaskStatus.New
                        && t.AssignedWorkerId == workerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.AssignedWorkerId, (string?)null)
                .SetProperty(t => t.ClaimedAt, (DateTime?)null));

        return released > 0;
    }

    public async Task<PickTask?> GetInProgressForContainerAsync(Guid containerId)
    {
        return await _context.PickTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContainerId == containerId && t.Status == PickTaskStatus.InProgress);
    }

    public async Task<PickTask?> GetMostRecentCompletedForContainerAsync(Guid containerId)
    {
        return await _context.PickTasks
            .AsNoTracking()
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Where(t => t.ContainerId == containerId && t.Status == PickTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PickTask?> GetByIdAsync(Guid id)
    {
        return await _context.PickTasks.FindAsync(id);
    }

    public async Task<PickTask?> GetByIdWithItemsAndProductLocationAsync(Guid id)
    {
        return await _context.PickTasks
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<PickTask?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.PickTasks
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<PickTask>> GetByOrderIdWithContainerLocationAsync(Guid orderId)
    {
        return await _context.PickTasks
            .Include(pt => pt.Container)
                .ThenInclude(c => c!.Location)
            .Where(pt => pt.OrderId == orderId)
            .ToListAsync();
    }

    public void Add(PickTask task)
    {
        _context.PickTasks.Add(task);
    }
}
