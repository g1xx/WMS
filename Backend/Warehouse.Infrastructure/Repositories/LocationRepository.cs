using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Location?> GetByIdAsync(Guid id)
    {
        return await _context.Locations.FindAsync(id);
    }

    public async Task<Location?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Locations.FirstOrDefaultAsync(l => l.AddressBarcode == barcode);
    }

    public async Task<Dictionary<string, Location>> GetByBarcodesAsync(List<string> barcodes)
    {
        return await _context.Locations
            .Where(l => barcodes.Contains(l.AddressBarcode))
            .ToDictionaryAsync(l => l.AddressBarcode);
    }

    public async Task<List<Location>> GetAllOrderedAsync()
    {
        return await _context.Locations
            .AsNoTracking()
            // The physical warehouse catalog. Transit locations are per-worker bookkeeping
            // rows, not places anyone walks to, manages, or puts stock away into, so they
            // don't belong in a location listing. Note this is the LOCATION catalog only —
            // stock sitting in transit stays visible in the stock listing (which includes
            // Location, so its Type identifies it), because hiding carried units would
            // make the stranded-stock case below even harder to spot.
            .Where(l => l.Type != LocationType.Transit)
            .OrderBy(l => l.Aisle)
            .ThenBy(l => l.Rack)
            .ToListAsync();
    }

    public async Task<Location?> GetTransitForWorkerAsync(string workerId)
    {
        return await _context.Locations
            .FirstOrDefaultAsync(l => l.Type == LocationType.Transit && l.AssignedWorkerId == workerId);
    }

    public async Task<Location> GetOrCreateTransitForWorkerAsync(string workerId, string displayName)
    {
        var existing = await GetTransitForWorkerAsync(workerId);
        if (existing != null) return existing;

        var location = new Location
        {
            Type = LocationType.Transit,
            AssignedWorkerId = workerId,
            AddressBarcode = $"TRANSIT-{displayName}",
            // Not a physical address. ZoneCode is derived from these and is meaningless
            // for a transit location — nothing routes to one (RouteOptimizerService can't
            // parse the barcode and sorts it last) and every stock query now excludes
            // Transit by type rather than by zone.
            WarehouseCode = "TRANSIT",
            Sector = "TRANSIT",
            Floor = 0,
            // Explicitly null: a worker's hands are not a storage slot. See
            // LocationCapacityDefaults, which says the same thing for the Type default.
            MaxDistinctSkus = null
        };

        _context.Locations.Add(location);

        try
        {
            await _context.SaveChangesAsync();
            return location;
        }
        catch (DbUpdateException)
        {
            // Lost the race: this worker opened relocation twice at once and the other
            // request created their transit location first. The filtered unique index on
            // AssignedWorkerId rejected this insert, which is exactly what should happen —
            // without it both would succeed and half their carried stock would end up in
            // an orphaned second transit location.
            _context.Entry(location).State = EntityState.Detached;

            return await GetTransitForWorkerAsync(workerId)
                   ?? throw new InvalidOperationException(
                       $"Transit location for worker '{workerId}' could not be created or found.");
        }
    }

    public void Add(Location location)
    {
        _context.Locations.Add(location);
    }

    public void AddRange(IEnumerable<Location> locations)
    {
        _context.Locations.AddRange(locations);
    }

    public async Task LockForUpdateAsync(Guid locationId)
    {
        // Result unused — the point is the side effect of holding the row lock for the
        // rest of the caller's transaction. AsNoTracking so this doesn't collide with
        // an already-tracked Location instance for the same Id elsewhere in the request.
        await _context.Set<Location>()
            .FromSqlInterpolated($"SELECT * FROM \"Locations\" WHERE \"Id\" = {locationId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();
    }
}
