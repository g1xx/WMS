using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId)
    {
        return await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId);
    }

    public async Task<List<Stock>> GetAvailableForProductsAsync(List<Guid> productIds)
    {
        return await _context.Stocks
            .Include(s => s.Location)
            .Where(s => productIds.Contains(s.ProductId) && (s.PhysicalQuantity - s.ReservedQuantity) > 0)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetReplacementCandidatesAsync(Guid productId, Guid excludeLocationId, string excludeSector)
    {
        return await _context.Stocks
            .Include(s => s.Location)
            .Where(s => s.ProductId == productId
                        && s.LocationId != excludeLocationId
                        && s.Location!.Sector != excludeSector
                        && (s.PhysicalQuantity - s.ReservedQuantity) > 0)
            .OrderByDescending(s => s.PhysicalQuantity - s.ReservedQuantity)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetAllWithDetailsAsync()
    {
        return await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Location)
            .ToListAsync();
    }

    public void Add(Stock stock)
    {
        _context.Stocks.Add(stock);
    }
}
