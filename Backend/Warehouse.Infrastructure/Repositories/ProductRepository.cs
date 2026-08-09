using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<bool> SkuExistsAsync(string sku)
    {
        return await _context.Products.AnyAsync(p => p.Sku == sku);
    }

    public async Task<Dictionary<string, Product>> GetBySkusAsync(List<string> skus)
    {
        return await _context.Products
            .Where(p => skus.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku);
    }

    public void Add(Product product)
    {
        _context.Products.Add(product);
    }
}
