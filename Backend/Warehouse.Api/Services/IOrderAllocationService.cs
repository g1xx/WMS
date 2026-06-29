namespace Warehouse.Api.Services;

public interface IOrderAllocationService
{
    Task<bool> AllocateOrderAsync(Guid orderId);
}