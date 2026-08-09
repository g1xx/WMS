using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<bool> SkuExistsAsync(string sku);

    // Batched — one query for every SKU in a putaway task, keyed by SKU for O(1)
    // lookup in the caller's loop (avoids the N+1 this replaced).
    Task<Dictionary<string, Product>> GetBySkusAsync(List<string> skus);

    void Add(Product product);
}
