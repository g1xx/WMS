using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IContainerRepository
{
    Task<Container?> GetByIdAsync(Guid id);
    Task<Container?> GetByIdWithLocationAsync(Guid id);
    Task<Container?> GetByBarcodeAsync(string barcode);
    Task<Container?> GetByBarcodeWithLocationAsync(string barcode);

    // Takes a row lock on this Container for the rest of the caller's transaction (a
    // raw SELECT ... FOR UPDATE) and returns its CURRENT status, bypassing the change
    // tracker entirely (AsNoTracking) — a caller-held tracked instance fetched earlier
    // in the same request would otherwise be stale, and GetByIdAsync's FindAsync would
    // silently return that cached value instead of hitting the database. Used by
    // ContainerLifecycleService before deciding whether a transition is still valid.
    // Returns null if the container doesn't exist.
    Task<ContainerStatus?> LockForUpdateAsync(Guid containerId);

    Task<bool> ExistsByBarcodeAsync(string barcode);

    // Batched — one query for a whole candidate range, so a bulk seeder can skip
    // barcodes that already exist instead of checking one at a time (N+1) or
    // relying on the unique index to reject duplicates as an error path.
    Task<HashSet<string>> GetExistingBarcodesAsync(List<string> barcodes);

    // Location included — feeds ContainersController's listings.
    Task<List<Container>> GetAllWithLocationAsync();
    Task<List<Container>> GetFreeWithLocationAsync();

    void Add(Container container);
    void AddRange(IEnumerable<Container> containers);
}
