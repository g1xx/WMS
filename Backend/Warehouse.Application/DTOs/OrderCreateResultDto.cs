using Warehouse.Domain;

namespace Warehouse.Application.DTOs;

// Response shape for OrdersController.CreateOrder. The order is created and saved
// before allocation is even attempted, so IsAllocated=false here (shortage, or an
// unexpected allocation failure) is never "order creation failed" — it always means
// the order exists, just not yet in Picking. See CreateOrder's own comment.
public class OrderCreateResultDto
{
    public Order Order { get; set; } = null!;
    public bool IsAllocated { get; set; }
    public string? AllocationMessage { get; set; }
}
