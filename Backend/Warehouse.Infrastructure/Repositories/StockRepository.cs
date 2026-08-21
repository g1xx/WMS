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

    public async Task<Dictionary<Guid, List<PutawaySuggestionCandidate>>> GetPutawaySuggestionCandidatesByProductAsync(List<Guid> productIds)
    {
        // No PhysicalQuantity > 0 filter here on purpose — a zero-quantity row is a
        // SKU's empty "home slot" and PutawayService needs to see it, not have it
        // silently excluded at the query level.
        var rows = await _context.Stocks
            .Include(s => s.Location)
            .Where(s => productIds.Contains(s.ProductId))
            .ToListAsync();

        return rows
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(s => new PutawaySuggestionCandidate
            {
                LocationId = s.LocationId,
                LocationBarcode = s.Location!.AddressBarcode,
                CurrentQuantity = s.PhysicalQuantity,
                ZoneCode = s.Location!.ZoneCode,
                LocationType = s.Location!.Type,
                MaxDistinctSkus = s.Location!.MaxDistinctSkus,
            }).ToList());
    }

    public async Task<Dictionary<Guid, int>> GetDistinctSkuCountsByLocationsAsync(List<Guid> locationIds)
    {
        return await _context.Stocks
            .Where(s => locationIds.Contains(s.LocationId) && s.PhysicalQuantity > 0)
            .GroupBy(s => s.LocationId)
            .Select(g => new { LocationId = g.Key, Count = g.Select(s => s.ProductId).Distinct().Count() })
            .ToDictionaryAsync(x => x.LocationId, x => x.Count);
    }

    public void Add(Stock stock)
    {
        _context.Stocks.Add(stock);
    }

    public async Task<int> CountDistinctProductsWithStockAtLocationAsync(Guid locationId)
    {
        return await _context.Stocks
            .Where(s => s.LocationId == locationId && s.PhysicalQuantity > 0)
            .Select(s => s.ProductId)
            .Distinct()
            .CountAsync();
    }
}
