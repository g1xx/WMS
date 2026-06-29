using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;
using System.ComponentModel.DataAnnotations;
using Warehouse.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Warehouse.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PickTaskController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PickTaskController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PickTaskResponseDto>>> GetPickTasks()
        {
            var tasks = await _context.PickTasks
                .AsNoTracking()
                .Select(t => new PickTaskResponseDto
                {
                    Id = t.Id,
                    Sector = t.Sector,
                    Status = t.Status.ToString(),
                    AssignedWorkerId = t.AssignedWorkerId,
                    Items = t.Items.Select(i => new PickTaskItemResponseDto
                    {
                        Id = i.Id,
                        LocationBarcode = i.Location!.AddressBarcode,
                        ProductName = i.Product!.Name,
                        ProductSku = i.Product.Sku,
                        RequiredQuantity = i.RequiredQuantity,
                        PickedQuantity = i.PickedQuantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost("{id}/start")]
        public async Task<ActionResult> StartPickTask(Guid id, StartPickTaskDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("Unable to determine user from token.");
            }

            var task = await _context.PickTasks.FindAsync(id);
            if (task == null) return NotFound("Pick task not found.");

            if (!string.IsNullOrEmpty(task.AssignedWorkerId) && task.AssignedWorkerId != currentUserId)
            {
                return BadRequest("Error! This task is already being performed by another worker.");
            }

            if (task.Status == PickTaskStatus.Completed)
            {
                return BadRequest("This task has already been fully picked.");
            }

            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.Barcode == dto.ContainerBarcode);

            if (container == null)
            {
                return BadRequest($"Container with barcode '{dto.ContainerBarcode}' not found.");
            }

            task.Status = PickTaskStatus.InProgress;
            task.AssignedWorkerId = currentUserId;
            task.ContainerId = container.Id;

            await _context.SaveChangesAsync();

            return Ok("Picking successfully started. Container linked, task locked to you.");
        }

        [HttpPost("{id}/pick")]
        public async Task<ActionResult> PickItem(Guid id, PickItemDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("Unable to determine user.");
            }

            var task = await _context.PickTasks
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound("Task not found.");

            if (task.Status != PickTaskStatus.InProgress)
            {
                return BadRequest("Cannot scan item: task is not active.");
            }

            if (task.AssignedWorkerId != currentUserId)
            {
                return BadRequest("Access error! The task is being performed by another worker.");
            }

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
            {
                return BadRequest("Scan error! You are at the wrong location or picked the wrong item.");
            }

            if (taskItem.PickedQuantity + dto.Quantity > taskItem.RequiredQuantity)
            {
                var leftToPick = taskItem.RequiredQuantity - taskItem.PickedQuantity;
                return BadRequest($"Over-pick! You only need to pick {leftToPick} more units of this item.");
            }

            taskItem.PickedQuantity += dto.Quantity;

            bool isTaskFinished = task.Items.All(i => i.PickedQuantity == i.RequiredQuantity);

            string resultMessage = $"Successfully picked: {dto.Quantity} units.";

            if (isTaskFinished)
            {
                task.Status = PickTaskStatus.Completed;
                resultMessage = "All items picked! Task completed. Container is ready for dispatch.";
            }

            await _context.SaveChangesAsync();

            return Ok(resultMessage);
        }
    }
}