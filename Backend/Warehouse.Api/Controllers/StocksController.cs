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
    public class StocksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StocksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            var stocks = await _context.Stocks
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Location)
                .ToListAsync();

            return Ok(stocks);
        }

        [HttpPost]
        public async Task<ActionResult> AddStock(StockCreateDto dto)
        {
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.AddressBarcode == dto.LocationBarcode);

            if (location == null)
            {
                return BadRequest($"Location with barcode '{dto.LocationBarcode}' was not found in the system.");
            }

            var existingStock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.LocationId == location.Id);

            if (existingStock != null)
            {
                existingStock.Quantity += dto.Quantity;
            }
            else
            {
                var newStock = new Stock
                {
                    ProductId = dto.ProductId,
                    LocationId = location.Id,
                    Quantity = dto.Quantity
                };
                _context.Stocks.Add(newStock);
            }

            await _context.SaveChangesAsync();
            return Ok("Product successfully received and placed in stock.");
        }
    }
}