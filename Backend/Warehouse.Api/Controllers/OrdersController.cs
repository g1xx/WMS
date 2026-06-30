using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;
using Warehouse.Api.Services;
using Warehouse.Api.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderAllocationService _orderAllocationService;

    public OrdersController(AppDbContext context, IOrderAllocationService orderAllocationService)
    {
        _context = context;
        _orderAllocationService = orderAllocationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound($"Order with ID {id} not found.");
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(OrderCreateDto dto)
    {
        var newOrder = new Order
        {
            OrderNumber = dto.OrderNumber,
            CustomerName = dto.CustomerName,
            DestinationAddress = dto.DestinationAddress,
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow,

            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                RequiredQuantity = i.RequiredQuantity,
                PickedQuantity = 0
            }).ToList()
        };

        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = newOrder.Id }, newOrder);
    }


    [HttpPost("{id}/allocate")]
    public async Task<ActionResult> AllocateOrder(Guid id)
    {
        try
        {
            var success = await _orderAllocationService.AllocateOrderAsync(id);

            if (!success)
            {
                return BadRequest("Cannot reserve order; it may not exist or is already in progress.");
            }

            return Ok("Order reserved, products allocated to locations, and status set to Picking.");

        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/pack")]
    public async Task<ActionResult> PackOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        if (order.Status != OrderStatus.Picking)
        {
            return BadRequest("Packing not possible. Order is not in the Picking status.");
        }

        var pickTasks = await _context.PickTasks
            .Include(pt => pt.Container)
                .ThenInclude(c => c!.Location)
            .Where(pt => pt.OrderId == id)
            .ToListAsync();

        if (!pickTasks.Any())
        {
            return BadRequest("There are no pick tasks for this order.");
        }

        if (pickTasks.Any(pt => pt.Status != PickTaskStatus.Completed))
        {
            return BadRequest("Cannot pack order: not all tasks are completed. Please wait for completion in all sectors.");
        }

        var failingTask = pickTasks.FirstOrDefault(pt =>
            pt.Container == null ||
            pt.Container.Location == null ||
            (int)pt.Container.Location.Type != 2);

        if (failingTask != null)
        {
            string debugMsg = $"ERROR DEBUG for task {failingTask.Id}:\n";
            debugMsg += $"- Container linked to task (Include worked)? {(failingTask.Container != null ? "Yes" : "NO (null)")}\n";
            debugMsg += $"- Container location found (ThenInclude worked)? {(failingTask.Container?.Location != null ? "Yes" : "NO (null)")}\n";

            if (failingTask.Container?.Location != null)
            {
                debugMsg += $"- Location barcode: {failingTask.Container.Location.AddressBarcode}\n";
                debugMsg += $"- Location type (integer): {(int)failingTask.Container.Location.Type}\n";
            }

            return BadRequest(debugMsg);
        }

        order.Status = OrderStatus.Packed;
        await _context.SaveChangesAsync();

        return Ok($"Order {order.OrderNumber} successfully consolidated, packed, and ready for shipment!");
    }
}