using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;

namespace Warehouse.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task MarkAsPickingAsync(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status != OrderStatus.Picking)
            {
                order.Status = OrderStatus.Picking;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateItemProgressAsync(Guid orderId, Guid productId, int pickedQuantity)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(oi => oi.ProductId == productId);
                if (orderItem != null)
                {
                    orderItem.PickedQuantity += pickedQuantity;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task CheckAndCloseOrderAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                // Check whether every item in the order has been picked
                bool isOrderFullyPicked = order.Items.All(i => i.PickedQuantity >= i.RequiredQuantity);
                if (isOrderFullyPicked)
                {
                    order.Status = OrderStatus.Packed;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}