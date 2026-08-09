using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class PutawayTaskRepository : IPutawayTaskRepository
{
    private readonly AppDbContext _context;

    public PutawayTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<PutawayTask> WithDetails() =>
        _context.PutawayTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.DestinationLocation);

    public async Task<PutawayTask?> GetByIdWithDetailsAsync(Guid id)
    {
        return await WithDetails().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<PutawayTask?> GetActiveForUserAsync(string workerId)
    {
        return await WithDetails()
            .FirstOrDefaultAsync(t => t.AssignedWorkerId == workerId && t.Status == PutawayTaskStatus.InProgress);
    }

    public async Task<PutawayTask?> GetInProgressForContainerSectorWorkerAsync(Guid containerId, string sector, string workerId)
    {
        return await WithDetails()
            .FirstOrDefaultAsync(t => t.ContainerId == containerId && t.Sector == sector
                                      && t.AssignedWorkerId == workerId && t.Status == PutawayTaskStatus.InProgress);
    }

    public async Task<PutawayTask?> GetNewForContainerSectorAsync(Guid containerId, string sector)
    {
        return await WithDetails()
            .FirstOrDefaultAsync(t => t.ContainerId == containerId && t.Sector == sector
                                      && t.Status == PutawayTaskStatus.New);
    }

    public async Task<List<PutawayTask>> GetPendingForContainerAsync(Guid containerId)
    {
        return await _context.PutawayTasks
            .Where(t => t.ContainerId == containerId &&
                        (t.Status == PutawayTaskStatus.New || t.Status == PutawayTaskStatus.InProgress))
            .ToListAsync();
    }

    public async Task<bool> HasOtherActiveTasksForContainerAsync(Guid containerId, Guid excludeTaskId)
    {
        return await _context.PutawayTasks
            .AnyAsync(t => t.ContainerId == containerId && t.Id != excludeTaskId
                           && t.Status != PutawayTaskStatus.Completed && t.Status != PutawayTaskStatus.Canceled);
    }

    public void Add(PutawayTask task)
    {
        _context.PutawayTasks.Add(task);
    }
}
