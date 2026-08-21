using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StocksController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            var stocks = await _unitOfWork.Stocks.GetAllWithDetailsAsync();

            return Ok(stocks);
        }

        [HttpPost]
        public async Task<ActionResult> AddStock(StockCreateDto dto)
        {
            if (dto.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            var location = await _unitOfWork.Locations.GetByBarcodeAsync(dto.LocationBarcode);

            if (location == null)
            {
                return BadRequest($"Location with barcode '{dto.LocationBarcode}' was not found in the system.");
            }

            var existingStock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(dto.ProductId, location.Id);

            if (existingStock != null)
            {
                existingStock.PhysicalQuantity += dto.Quantity;
            }
            else
            {
                var newStock = new Stock
                {
                    ProductId = dto.ProductId,
                    LocationId = location.Id,
                    PhysicalQuantity = dto.Quantity,
                    ReservedQuantity = 0
                };
                _unitOfWork.Stocks.Add(newStock);
            }

            await _unitOfWork.SaveChangesAsync();
            return Ok("Product successfully received and placed in stock.");
        }
    }
}
