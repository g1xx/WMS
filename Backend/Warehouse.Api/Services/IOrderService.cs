namespace Warehouse.Api.Services
{
    public interface IOrderService
    {
        Task MarkAsPickingAsync(Guid orderId);
        Task UpdateItemProgressAsync(Guid orderId, Guid productId, int pickedQuantity);
        Task CheckAndCloseOrderAsync(Guid orderId);
    }
}
