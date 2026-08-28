using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Common;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers;

// Read-only lookup for the terminal's "Informacja o..." screen. Nothing here mutates.
//
// AnyStaff, and deliberately NOT reachable by the Integration feed: these endpoints expose
// warehouse layout, per-location stock positions and live container statuses, which is
// precisely the internal detail GET /api/Products/for-ordering exists to keep away from an
// upstream system. Callers checked in both frontends — only warehouse-client calls these;
// TestOrderGenerator does not. See the note on RoleNames.AnyStaff.
[Authorize(Roles = RoleNames.AnyStaff)]
[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    private readonly IInfoService _infoService;

    public InfoController(IInfoService infoService)
    {
        _infoService = infoService;
    }

    // By SKU only — Product has no barcode column; Sku is the scanned identifier
    // everywhere in this system.
    [HttpGet("product/{sku}")]
    public async Task<IActionResult> GetProduct(string sku)
    {
        var result = await _infoService.GetProductInfoAsync(sku.Trim());
        return result.ToActionResult();
    }

    [HttpGet("container/{barcode}")]
    public async Task<IActionResult> GetContainer(string barcode)
    {
        var result = await _infoService.GetContainerInfoAsync(barcode.Trim());
        return result.ToActionResult();
    }

    [HttpGet("location/{barcode}")]
    public async Task<IActionResult> GetLocation(string barcode)
    {
        var result = await _infoService.GetLocationInfoAsync(barcode.Trim());
        return result.ToActionResult();
    }
}
