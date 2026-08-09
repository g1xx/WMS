using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Common;
using Warehouse.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Warehouse.Application.Services;

namespace Warehouse.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PutawayTaskController : ControllerBase
    {
        private readonly IPutawayService _putawayService;

        public PutawayTaskController(IPutawayService putawayService)
        {
            _putawayService = putawayService;
        }

        // Registers a container's expected inbound contents. Stands in for a
        // receiving/inbound flow this system doesn't have yet — see PutawayService.
        // Unauthenticated on purpose, same as OrdersController.CreateOrder: this is
        // a seeding/setup action for tooling (e.g. the test data generator), not a
        // worker-facing terminal action like the rest of this controller.
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> CreatePutawayTask([FromBody] CreatePutawayTaskDto dto)
        {
            var result = await _putawayService.CreatePutawayTaskAsync(dto);
            return result.ToActionResult();
        }

        // The worker's own in-flight task, regardless of sector — lets a re-login
        // resume straight back into it, same as PickTask/active.
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveTask()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var task = await _putawayService.GetActivePutawayTaskForUserAsync(userId);
            return Ok(task);
        }

        [HttpPost("validate-container")]
        public async Task<ActionResult> ValidateContainer([FromBody] ValidateContainerDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            if (string.IsNullOrWhiteSpace(dto.Sector))
                return BadRequest("A sector is required to validate a container.");

            var result = await _putawayService.ValidateContainerAsync(dto.ContainerBarcode, dto.Sector);
            return result.ToActionResult();
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartPutaway([FromBody] ValidateContainerDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            if (string.IsNullOrWhiteSpace(dto.Sector))
                return BadRequest("A sector is required to start putaway.");

            var result = await _putawayService.StartPutawayForContainerAsync(dto.ContainerBarcode, dto.Sector, userId);
            return result.ToActionResult();
        }

        [HttpPost("{id}/confirm-item")]
        public async Task<ActionResult> ConfirmItem(Guid id, [FromBody] ConfirmPutawayItemDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _putawayService.ConfirmItemAsync(id, dto, userId);
            return result.ToActionResult();
        }

        // Supervisor-only: confirms a shortage on a task assigned to some worker,
        // not necessarily the caller — see PutawayService.ReportMissingAsync.
        [Authorize(Roles = "Brigadier,Admin")]
        [HttpPost("{id}/report-missing")]
        public async Task<ActionResult> ReportMissing(Guid id, [FromBody] ReportPutawayMissingDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _putawayService.ReportMissingAsync(id, dto, userId);
            return result.ToActionResult();
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}
