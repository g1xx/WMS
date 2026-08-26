using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Warehouse.Api.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers;

// Stock relocation between locations. Every action is scoped to the calling worker's own
// transit location — there is no way to address someone else's, because the worker id
// always comes from the token, never from the request body.
[Authorize(Roles = RoleNames.AnyStaff)]
[ApiController]
[Route("api/[controller]")]
public class RelocationController : ControllerBase
{
    private readonly IRelocationService _relocationService;

    public RelocationController(IRelocationService relocationService)
    {
        _relocationService = relocationService;
    }

    // Also creates the worker's transit location on first call, so the client can open
    // the flow without a separate "start" step.
    [HttpGet("state")]
    public async Task<IActionResult> GetState()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

        return Ok(await _relocationService.GetStateAsync(userId, GetDisplayName(userId)));
    }

    // Backs "press Enter with no product scanned to see what's here".
    [HttpGet("location/{barcode}")]
    public async Task<IActionResult> GetLocationContents(string barcode)
    {
        var result = await _relocationService.GetLocationContentsAsync(barcode.Trim());
        return result.ToActionResult();
    }

    [HttpPost("take")]
    public async Task<IActionResult> Take([FromBody] RelocationTakeDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

        var result = await _relocationService.TakeAsync(userId, GetDisplayName(userId), dto);
        return result.ToActionResult();
    }

    [HttpPost("putaway")]
    public async Task<IActionResult> PutAway([FromBody] RelocationPutawayDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

        var result = await _relocationService.PutAwayAsync(userId, GetDisplayName(userId), dto);
        return result.ToActionResult();
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }

    // Only ever used to build a readable transit barcode (TRANSIT-{name}). Identity comes
    // from the user id, so a later username change leaves the existing barcode alone
    // rather than orphaning the worker from their carried stock.
    private string GetDisplayName(string userId)
    {
        return User.Identity?.Name
               ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
               ?? userId;
    }
}
