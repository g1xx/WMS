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
    // EXCLUDES transit locations: they're per-worker bookkeeping, not physical places.
    Task<List<Location>> GetAllOrderedAsync();

    // This worker's transit location, or null if they've never started a relocation.
    Task<Location?> GetTransitForWorkerAsync(string workerId);

    // This worker's transit location, creating it on first use. Lives in the repository
    // rather than the service because losing the create race has to be caught as a
    // unique-constraint violation, and the Application layer has no EF Core reference to
    // catch one with.
    //
    // MUST be called OUTSIDE a transaction. It saves, and in Postgres a failed INSERT
    // aborts the surrounding transaction — the re-read after losing the race would then
    // fail too. Callers do this once, up front, before opening their own transaction.
    Task<Location> GetOrCreateTransitForWorkerAsync(string workerId, string displayName);

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
