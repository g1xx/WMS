using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id);
    Task<Location?> GetByBarcodeAsync(string barcode);

    // Batched — one query for every destination barcode in a putaway task, keyed by
    // barcode for O(1) lookup in the caller's loop (avoids the N+1 this replaced).
    Task<Dictionary<string, Location>> GetByBarcodesAsync(List<string> barcodes);

    // Ordered by Aisle then Rack — matches LocationsController's catalog listing.
    Task<List<Location>> GetAllOrderedAsync();

    void Add(Location location);
    void AddRange(IEnumerable<Location> locations);
}
