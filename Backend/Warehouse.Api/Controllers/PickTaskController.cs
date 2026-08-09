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
    public class PickTaskController : ControllerBase
    {
        private readonly IPickTaskService _pickTaskService;

        // Inject the service instead of AppDbContext
        public PickTaskController(IPickTaskService pickTaskService)
        {
            _pickTaskService = pickTaskService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PickTaskResponseDto>>> GetPickTasks()
        {
            var tasks = await _pickTaskService.GetPickTasksAsync();
            return Ok(tasks);
        }

        // The worker's own in-flight task, regardless of sector. Called before sector
        // selection so a re-login can resume straight into an already-started task.
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveTask()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var task = await _pickTaskService.GetActiveTaskForUserAsync(userId);

            return Ok(task);
        }

        [HttpGet("next")]
        public async Task<IActionResult> GetNextTask([FromQuery] string sector)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            if (string.IsNullOrWhiteSpace(sector))
                return BadRequest("A sector is required to request the next task.");

            var task = await _pickTaskService.GetNextTaskAsync(userId, sector.Trim());

            return Ok(task);
        }

        [HttpPost("{id}/start")]
        public async Task<ActionResult> StartPickTask(Guid id, StartPickTaskDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _pickTaskService.StartPickTaskAsync(id, dto, userId);
            return result.ToActionResult();
        }

        [HttpPost("{id}/pick")]
        public async Task<ActionResult> PickItem(Guid id, PickItemDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _pickTaskService.PickItemAsync(id, dto, userId);
            return result.ToActionResult();
        }

        [HttpPost("{id}/dispatch")]
        public async Task<ActionResult> DispatchContainer(Guid id, [FromBody] DispatchContainerDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            // Handles both the normal close-out and the "Full container" case
            var result = await _pickTaskService.DispatchContainerAsync(id, dto, userId);
            return result.ToActionResult();
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelTask(Guid id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _pickTaskService.CancelPickTaskAsync(id, userId);
            return result.ToActionResult();
        }

        // Supervisor-only: confirms a shortage on a task assigned to some worker,
        // not necessarily the caller — see PickTaskService.ReportMissingItemAsync.
        [Authorize(Roles = "Brigadier,Admin")]
        [HttpPost("{id}/report-missing")]
        public async Task<ActionResult> ReportMissingItem(Guid id, [FromBody] ReportMissingItemDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _pickTaskService.ReportMissingItemAsync(id, dto, userId);
            return result.ToActionResult();
        }

        // Supervisor-only: confirms a defect on a task assigned to some worker,
        // not necessarily the caller — see PickTaskService.ReportDefectAsync.
        [Authorize(Roles = "Brigadier,Admin")]
        [HttpPost("{id}/report-defect")]
        public async Task<ActionResult> ReportDefect(Guid id, [FromBody] ReportDefectDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            var result = await _pickTaskService.ReportDefectAsync(id, dto, userId);
            return result.ToActionResult();
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}