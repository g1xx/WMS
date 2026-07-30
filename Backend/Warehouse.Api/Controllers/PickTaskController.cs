using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.DTOs;
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

        // Подключаем наш сервис вместо AppDbContext!
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

        // ВОССТАНОВИЛИ метод получения следующего задания
        [HttpGet("next")]
        public async Task<IActionResult> GetNextTask()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Не удалось определить пользователя.");

            var task = await _pickTaskService.GetNextTaskAsync(userId);

            if (task == null) return Ok(null);

            return Ok(task);
        }

        [HttpPost("{id}/start")]
        public async Task<ActionResult> StartPickTask(Guid id, StartPickTaskDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            try
            {
                var message = await _pickTaskService.StartPickTaskAsync(id, dto, userId);
                return Ok(message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/pick")]
        public async Task<ActionResult> PickItem(Guid id, PickItemDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            try
            {
                var message = await _pickTaskService.PickItemAsync(id, dto, userId);
                return Ok(message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ДОБАВИЛИ ТОТ САМЫЙ МЕТОД, ИЗ-ЗА КОТОРОГО БЫЛА ОШИБКА 404
        [HttpPost("{id}/dispatch")]
        public async Task<ActionResult> DispatchContainer(Guid id, [FromBody] DispatchContainerDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            try
            {
                // Вызываем метод из сервиса, который обрабатывает и штатное закрытие, и "Полный контейнер"
                var newTaskId = await _pickTaskService.DispatchContainerAsync(id, dto, userId);

                return Ok(new
                {
                    Message = "Контейнер успешно проверен и отправлен на конвейер.",
                    NextTaskId = newTaskId
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelTask(Guid id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            try
            {
                var message = await _pickTaskService.CancelPickTaskAsync(id, userId);
                return Ok(new { Message = message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/report-missing")]
        public async Task<ActionResult> ReportMissingItem(Guid id, [FromBody] ReportMissingItemDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Unable to determine user.");

            try
            {
                var message = await _pickTaskService.ReportMissingItemAsync(id, dto, userId);
                return Ok(new { Message = message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}