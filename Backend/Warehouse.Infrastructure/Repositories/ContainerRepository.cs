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

    public async Task<ContainerStatus?> LockForUpdateAsync(Guid containerId)
    {
        // Container (unlike Location) has UseXminAsConcurrencyToken() configured, and
        // xmin is a Postgres system column — "SELECT *" doesn't include it, so EF Core
        // can't materialize the entity without it being selected explicitly.
        var rows = await _context.Set<Container>()
            .FromSqlInterpolated($"SELECT *, xmin FROM \"Containers\" WHERE \"Id\" = {containerId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();

        return rows.FirstOrDefault()?.Status;
    }

    public async Task<bool> ExistsByBarcodeAsync(string barcode)
    {
        return await _context.Containers.AnyAsync(c => c.Barcode == barcode);
    }

    public async Task<HashSet<string>> GetExistingBarcodesAsync(List<string> barcodes)
    {
        var existing = await _context.Containers
            .Where(c => barcodes.Contains(c.Barcode))
            .Select(c => c.Barcode)
            .ToListAsync();

        return existing.ToHashSet();
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
            .Where(c => c.Status == ContainerTransitions.FreeStatus)
            .Include(c => c.Location)
            .AsNoTracking()
            .ToListAsync();
    }

    public void Add(Container container)
    {
        _context.Containers.Add(container);
    }

    public void AddRange(IEnumerable<Container> containers)
    {
        _context.Containers.AddRange(containers);
    }
}
