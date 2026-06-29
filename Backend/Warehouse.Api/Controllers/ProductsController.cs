using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;
using System.ComponentModel.DataAnnotations;
using Warehouse.Api.DTOs;

namespace Warehouse.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        //{
        //    var products = await _context.Products
        //        .AsNoTracking()
        //        .Include(p => p.Stocks)
        //        .ToListAsync();

        //    return Ok(products);
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts([FromQuery] DateTime? since)
        {
            var query = _context.Products.AsNoTracking();

            if (since.HasValue)
            {
                query = query.Where(p => p.UpdatedAt > since.Value);
            }

            var products = await query
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    SizeCategory = p.SizeCategory.ToString(),
                    Stocks = p.Stocks.Select(s => new StockCreateDto
                    {
                        ProductId = s.ProductId,
                        LocationBarcode = s.Location != null ? s.Location.AddressBarcode : "NO_LOCATION",
                        Quantity = s.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(ProductCreateDto dto)
        {
            var newProduct = new Product
            {
                Name = dto.Name,
                Sku = dto.Sku,
                Price = dto.Price,
                WeightKg = dto.WeightKg,
                LengthCm = dto.LengthCm,
                WidthCm = dto.WidthCm,
                HeightCm = dto.HeightCm,
                BaseUnit = (UnitType)dto.BaseUnit,
                ItemPerPackage = dto.ItemPerPackage
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            var responseDto = new ProductResponseDto
            {
                Id = newProduct.Id,
                Name = newProduct.Name,
                Sku = newProduct.Sku,
                SizeCategory = newProduct.SizeCategory.ToString()
            };

            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, responseDto);
        }
    }
}
