using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;

namespace Warehouse.Api.Services;

public class OrderAllocationService : IOrderAllocationService
{
    private readonly AppDbContext _context;

    public OrderAllocationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AllocateOrderAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status != OrderStatus.New) return false;

        var plannedPicks = new List<(Guid ProductId, Guid LocationId, string Sector, int Quantity)>();

        foreach (var item in order.Items)
        {
            var remainingToAllocate = item.RequiredQuantity;

            var availableStocks = await _context.Stocks
                .Include(s => s.Location)
                .Where(s => s.ProductId == item.ProductId && s.Quantity > 0)
                .OrderBy(s => s.Quantity)
                .ToListAsync();

            foreach (var stock in availableStocks)
            {
                if (remainingToAllocate == 0) break;

                var quantityToTake = Math.Min(remainingToAllocate, stock.Quantity);

                stock.Quantity -= quantityToTake;
                remainingToAllocate -= quantityToTake;

                plannedPicks.Add((item.ProductId, stock.LocationId, stock.Location!.Sector, quantityToTake));
            }

            if (remainingToAllocate > 0)
            {
                throw new Exception($"Warehouse deficit! {remainingToAllocate} units missing for product {item.ProductId}");
            }
        }

        var picksBySector = plannedPicks.GroupBy(p => p.Sector);

        foreach (var sectorGroup in picksBySector)
        {
            var pickTask = new PickTask
            {
                OrderId = order.Id,
                Sector = sectorGroup.Key,
                Status = PickTaskStatus.New,
                Items = sectorGroup.Select(p => new PickTaskItem
                {
                    ProductId = p.ProductId,
                    LocationId = p.LocationId,
                    RequiredQuantity = p.Quantity,
                    PickedQuantity = 0
                }).ToList()
            };

            _context.PickTasks.Add(pickTask);
        }

        order.Status = OrderStatus.Picking;
        await _context.SaveChangesAsync();

        return true;
    }
}