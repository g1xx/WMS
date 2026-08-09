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

    public void Add(Location location)
    {
        _context.Locations.Add(location);
    }
}
