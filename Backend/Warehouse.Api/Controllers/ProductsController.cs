using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers
{
    // Had NO authorization at all until now — every action here was anonymous, and
    // GetProduct returns the raw Product entity including Price, so the full catalog was
    // readable from the internet without a token.
    //
    // Bare [Authorize] at the class level with roles on each action, matching
    // OrdersController and PutawayTaskController: this controller serves BOTH staff and the
    // Integration feed, and those need different roles. Multiple [Authorize] attributes are
    // AND-ed, so a class-level Roles = AnyStaff would combine with an action-level
    // Roles = Integration to produce an endpoint reachable by nobody at all.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Roles = RoleNames.AnyStaff)]
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

        [Authorize(Roles = RoleNames.AnyStaff)]
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

        // The Integration feed's view of the catalogue: enough to name a product on an
        // order line and know how much may be ordered, with no warehouse layout attached.
        //
        // Exists because the feed used to call GetProducts above, which hands out a
        // per-location stock breakdown. Locking that action to AnyStaff broke the feed
        // (Integration is excluded from AnyStaff by design), and the fix is a narrower
        // endpoint rather than widening the staff one — an upstream system needs to name
        // what it is ordering, not to learn which shelf holds it.
        [Authorize(Roles = RoleNames.Integration)]
        [HttpGet("for-ordering")]
        public async Task<ActionResult<IEnumerable<OrderableProductDto>>> GetProductsForOrdering()
        {
            var products = await _unitOfWork.Products.GetAllWithStocksAsync();

            var responseDtos = products.Select(p => new OrderableProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                // Transit excluded for the same reason OrderAllocationService excludes it:
                // stock in a worker's hands is not allocatable, so offering it here would
                // invite orders that can never be filled from it.
                AvailableQuantity = p.Stocks
                    .Where(s => s.Location == null || s.Location.Type != LocationType.Transit)
                    .Sum(s => s.PhysicalQuantity - s.ReservedQuantity)
            }).ToList();

            return Ok(responseDtos);
        }

        [Authorize(Roles = RoleNames.AnyStaff)]
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
