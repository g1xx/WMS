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
        return await _context.PickTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            .FirstOrDefaultAsync(t => t.AssignedWorkerId == userId && t.Status == PickTaskStatus.InProgress);
    }

    public async Task<PickTask?> GetNextForSectorAsync(string sector)
    {
        return await _context.PickTasks
            .Include(t => t.Container)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
            .FirstOrDefaultAsync(t => t.Status == PickTaskStatus.New && t.AssignedWorkerId == null && t.Sector == sector);
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
