namespace Warehouse.Application.Services;

public interface IOrderAllocationService
{
    Task<(bool IsAllocated, string? Message)> AllocateOrderAsync(Guid orderId);
}
