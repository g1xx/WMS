using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderAllocationService _orderAllocationService;

    public OrdersController(IUnitOfWork unitOfWork, IOrderAllocationService orderAllocationService)
    {
        _unitOfWork = unitOfWork;
        _orderAllocationService = orderAllocationService;
    }

    [Authorize(Roles = RoleNames.AnyStaff)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _unitOfWork.Orders.GetAllWithItemsAsync();

        return Ok(orders);
    }

    [Authorize(Roles = RoleNames.AnyStaff)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(id);

        if (order == null)
        {
            return NotFound($"Order with ID {id} not found.");
        }

        return Ok(order);
    }

    // No Roles restriction beyond the class-level [Authorize] — this is the one action
    // the Integration role (an upstream ERP/marketplace feed) is granted, alongside
    // PutawayTaskController's create action. See RoleNames.Integration.
    [HttpPost]
    public async Task<ActionResult> CreateOrder(OrderCreateDto dto)
    {
        if (dto.Items.Any(i => i.RequiredQuantity <= 0))
        {
            return BadRequest("Required quantity must be greater than zero for every item.");
        }

        var newOrder = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
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

        _unitOfWork.Orders.Add(newOrder);
        await _unitOfWork.SaveChangesAsync();

        // The order is durably committed above already, so nothing below can make order
        // creation itself "fail" — a shortage isn't an error (OrderAllocationService parks
        // the order as AwaitingReplenishment and returns IsAllocated=false, same as it would
        // for a manual /allocate call), and even a genuine unexpected exception here must
        // not roll back or hide the order that was just created. The Integration role (see
        // RoleNames.Integration) has no access to the separate /allocate endpoint, so this
        // is the only place its orders ever get a chance to reach Picking.
        bool isAllocated;
        string? allocationMessage;
        try
        {
            (isAllocated, allocationMessage) = await _orderAllocationService.AllocateOrderAsync(newOrder.Id);
        }
        catch (Exception ex)
        {
            isAllocated = false;
            allocationMessage = $"Order was created, but allocation failed unexpectedly: {ex.Message}";
        }

        var result = new OrderCreateResultDto
        {
            Order = newOrder,
            IsAllocated = isAllocated,
            AllocationMessage = allocationMessage,
        };

        return CreatedAtAction(nameof(GetOrder), new { id = newOrder.Id }, result);
    }


    [Authorize(Roles = RoleNames.AnyStaff)]
    [HttpPost("{id}/allocate")]
    public async Task<ActionResult> AllocateOrder(Guid id)
    {
        var result = await _orderAllocationService.AllocateOrderAsync(id);

        if (!result.IsAllocated)
        {
            // A shortage (or an order that is not allocatable) is a valid domain
            // state rather than a malformed request, so report it as a conflict.
            return Conflict(result.Message ?? "Cannot reserve order; it may not exist or is already in progress.");
        }

        return Ok("Order reserved, products allocated to locations, and status set to Picking.");
    }

    [Authorize(Roles = RoleNames.AnyStaff)]
    [HttpPost("{id}/pack")]
    public async Task<ActionResult> PackOrder(Guid id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        if (order.Status != OrderStatus.Picking)
        {
            return BadRequest("Packing not possible. Order is not in the Picking status.");
        }

        var pickTasks = await _unitOfWork.PickTasks.GetByOrderIdWithContainerLocationAsync(id);

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
            pt.Container.Location.Type != LocationType.DockDoor);

        if (failingTask != null)
        {
            return BadRequest($"Cannot pack order: task {failingTask.Id}'s container is not staged at a dock door.");
        }

        order.Status = OrderStatus.Packed;
        await _unitOfWork.SaveChangesAsync();

        return Ok($"Order {order.OrderNumber} successfully consolidated, packed, and ready for shipment!");
    }
}
