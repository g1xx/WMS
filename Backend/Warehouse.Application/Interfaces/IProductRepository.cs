using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<bool> SkuExistsAsync(string sku);

    // Batched — one query for every SKU in a putaway task, keyed by SKU for O(1)
    // lookup in the caller's loop (avoids the N+1 this replaced).
    Task<Dictionary<string, Product>> GetBySkusAsync(List<string> skus);

    // Stocks.Location included — feeds ProductsController's catalog listing (with its
    // per-location available-quantity breakdown), optionally filtered to recently updated.
    Task<List<Product>> GetAllWithStocksAsync(DateTime? updatedSince = null);

    void Add(Product product);
}
