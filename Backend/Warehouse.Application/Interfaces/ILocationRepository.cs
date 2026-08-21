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

    // Takes a row lock on this Location for the rest of the caller's transaction (a
    // raw SELECT ... FOR UPDATE — EF Core has no fluent way to express this). Used
    // before checking/enforcing MaxDistinctSkus: without it, two concurrent putaway
    // confirms into the same near-full location could both read "room for one more"
    // before either commits. A second concurrent caller locking the same Id blocks
    // here until the first transaction commits or rolls back.
    Task LockForUpdateAsync(Guid locationId);
}
