using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Common;
using Warehouse.Api.DTOs;
using Warehouse.Api.Services;

namespace Warehouse.Api.Controllers;

// Backing endpoints for the Admin/Inventory tab: manual stock corrections and
// creating a new product with its first stock location bound in one step.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("adjust-stock")]
    public async Task<ActionResult> AdjustStock([FromBody] AdjustStockDto dto)
    {
        var result = await _inventoryService.AdjustPhysicalStockAsync(dto.ProductId, dto.LocationBarcode, dto.QuantityDelta, dto.Reason);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(result.Error),
                ResultErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    [HttpPost("products")]
    public async Task<ActionResult> CreateProduct([FromBody] CreateProductWithLocationDto dto)
    {
        var result = await _inventoryService.CreateProductWithLocationAsync(dto);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(result.Error),
                ResultErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }
}
