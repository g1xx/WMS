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

    public async Task<(int PhysicalQuantity, int ReservedQuantity)?> LockForUpdateAsync(Guid productId, Guid locationId)
    {
        // Stock has UseXminAsConcurrencyToken() configured, and xmin is a Postgres system
        // column — "SELECT *" doesn't include it, so EF Core cannot materialize the entity
        // without selecting it explicitly. Exactly the same trap as ContainerRepository's
        // equivalent; getting it wrong fails at materialization, not at compile time.
        //
        // AsNoTracking so this is the committed row, not whatever stale instance the
        // caller's earlier read already put in the change tracker.
        var rows = await _context.Set<Stock>()
            .FromSqlInterpolated($@"
                SELECT *, xmin FROM ""Stocks""
                WHERE ""ProductId"" = {productId} AND ""LocationId"" = {locationId}
                FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();

        var row = rows.FirstOrDefault();
        return row == null ? null : (row.PhysicalQuantity, row.ReservedQuantity);
    }

    public async Task<List<Stock>> GetWithProductAtLocationAsync(Guid locationId)
    {
        return await _context.Stocks
            .Include(s => s.Product)
            .Where(s => s.LocationId == locationId && s.PhysicalQuantity > 0)
            .OrderBy(s => s.Product!.Sku)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetAvailableForProductsAsync(List<Guid> productIds)
    {
        return await _context.Stocks
            .Include(s => s.Location)
            // Stock in a worker's hands is not allocatable. Without this, an order would
            // reserve units someone is physically carrying and OrderAllocationService
            // would build a pick task in the transit "zone" — dispatching a picker to
            // another worker's hands.
            .Where(s => s.Location!.Type != LocationType.Transit)
            .Where(s => productIds.Contains(s.ProductId) && (s.PhysicalQuantity - s.ReservedQuantity) > 0)
            .ToListAsync();
    }

    public async Task<List<Stock>> GetReplacementCandidatesAsync(Guid productId, Guid excludeLocationId, string excludeSector)
    {
        return await _context.Stocks
            .Include(s => s.Location)
            // Filtered here rather than relying on UnfulfillableUnitHandler's
            // ActivePickingZones allowlist to exclude transit by omission. That allowlist
            // happens to keep transit out today, but it's a list of zone codes someone
            // will eventually add to, and nothing about it says "and never a transit
            // location" — an exclusion that survives only by coincidence isn't one.
            .Where(s => s.Location!.Type != LocationType.Transit)
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
            // A transit location is never a putaway destination — suggesting one would
            // offer the worker their own hands, or somebody else's.
            .Where(s => s.Location!.Type != LocationType.Transit)
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
