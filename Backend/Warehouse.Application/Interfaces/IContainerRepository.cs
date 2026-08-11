using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IContainerRepository
{
    Task<Container?> GetByIdAsync(Guid id);
    Task<Container?> GetByIdWithLocationAsync(Guid id);
    Task<Container?> GetByBarcodeAsync(string barcode);
    Task<Container?> GetByBarcodeWithLocationAsync(string barcode);

    // Status == New || Available — only ever suggest containers that are actually free.
    Task<Container?> GetFreeByBarcodeAsync(string barcode);

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
