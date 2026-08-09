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

    // Location included — feeds ContainersController's listings.
    Task<List<Container>> GetAllWithLocationAsync();
    Task<List<Container>> GetFreeWithLocationAsync();

    void Add(Container container);
}
