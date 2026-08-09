using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class ContainerRepository : IContainerRepository
{
    private readonly AppDbContext _context;

    public ContainerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Container?> GetByIdAsync(Guid id)
    {
        return await _context.Containers.FindAsync(id);
    }

    public async Task<Container?> GetByIdWithLocationAsync(Guid id)
    {
        return await _context.Containers
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Container?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Containers.FirstOrDefaultAsync(c => c.Barcode == barcode);
    }

    public async Task<Container?> GetByBarcodeWithLocationAsync(string barcode)
    {
        return await _context.Containers
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.Barcode == barcode);
    }

    public async Task<Container?> GetFreeByBarcodeAsync(string barcode)
    {
        return await _context.Containers
            .Where(c => c.Status == ContainerStatus.New || c.Status == ContainerStatus.Available)
            .FirstOrDefaultAsync(c => c.Barcode == barcode);
    }

    public async Task<bool> ExistsByBarcodeAsync(string barcode)
    {
        return await _context.Containers.AnyAsync(c => c.Barcode == barcode);
    }

    public async Task<List<Container>> GetAllWithLocationAsync()
    {
        return await _context.Containers
            .Include(c => c.Location)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Container>> GetFreeWithLocationAsync()
    {
        return await _context.Containers
            .Where(c => c.Status == ContainerStatus.New || c.Status == ContainerStatus.Available)
            .Include(c => c.Location)
            .AsNoTracking()
            .ToListAsync();
    }

    public void Add(Container container)
    {
        _context.Containers.Add(container);
    }
}
