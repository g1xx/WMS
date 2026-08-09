using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts([FromQuery] DateTime? since)
        {
            var products = await _unitOfWork.Products.GetAllWithStocksAsync(since);

            var responseDtos = products.Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                SizeCategory = p.SizeCategory.ToString(),
                Stocks = p.Stocks.Select(s => new StockCreateDto
                {
                    ProductId = s.ProductId,
                    LocationBarcode = s.Location != null ? s.Location.AddressBarcode : "NO_LOCATION",
                    Quantity = s.PhysicalQuantity - s.ReservedQuantity
                }).ToList()
            }).ToList();

            return Ok(responseDtos);
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

            _unitOfWork.Products.Add(newProduct);
            await _unitOfWork.SaveChangesAsync();

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
