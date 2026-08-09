using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IContainerRepository
{
    Task<Container?> GetByIdAsync(Guid id);
    Task<Container?> GetByBarcodeAsync(string barcode);

    // Status == New || Available — only ever suggest containers that are actually free.
    Task<Container?> GetFreeByBarcodeAsync(string barcode);

    Task<bool> ExistsByBarcodeAsync(string barcode);

    void Add(Container container);
}
