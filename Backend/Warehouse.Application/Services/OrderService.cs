using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task MarkAsPickingAsync(Guid orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order != null && order.Status != OrderStatus.Picking)
            {
                order.Status = OrderStatus.Picking;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateItemProgressAsync(Guid orderId, Guid productId, int pickedQuantity)
        {
            var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(orderId);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(oi => oi.ProductId == productId);
                if (orderItem != null)
                {
                    orderItem.PickedQuantity += pickedQuantity;
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        public async Task CheckAndCloseOrderAsync(Guid orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(orderId);

            if (order != null)
            {
                // Check whether every item in the order has been picked
                bool isOrderFullyPicked = order.Items.All(i => i.PickedQuantity >= i.RequiredQuantity);
                if (isOrderFullyPicked)
                {
                    order.Status = OrderStatus.Packed;
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
}
