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

    public async Task<(bool IsAllocated, string? Message)> AllocateOrderAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return (false, $"Order {orderId} was not found.");
        }

        if (order.Status != OrderStatus.New)
        {
            return (false, $"Order {order.OrderNumber} cannot be allocated: current status is {order.Status}, expected {OrderStatus.New}.");
        }

        // ----------------------------------------------------------------
        // Phase 1: validation / dry run.
        // Build the full pick plan without touching ReservedQuantity, so a
        // shortage leaves no partial reservations behind.
        // ----------------------------------------------------------------
        var plannedPicks = new List<(Stock Stock, Guid ProductId, Guid LocationId, string ZoneCode, int Quantity)>();

        // stock row id -> quantity already earmarked earlier in this same run.
        // Needed because two order items can draw on the same stock row, and
        // during the dry run ReservedQuantity does not yet reflect the plan.
        var earmarked = new Dictionary<Guid, int>();

        // Batch-fetch every candidate stock row for every product in the order up front,
        // instead of running one query per order item (N+1).
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        var candidateStocks = await _context.Stocks
            .Include(s => s.Location)
            .Where(s => productIds.Contains(s.ProductId) && (s.PhysicalQuantity - s.ReservedQuantity) > 0)
            .ToListAsync();

        var stocksByProduct = candidateStocks
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.AvailableQuantity).ToList());

        foreach (var item in order.Items)
        {
            var remainingToAllocate = item.RequiredQuantity;

            stocksByProduct.TryGetValue(item.ProductId, out var availableStocks);
            availableStocks ??= new List<Stock>();

            foreach (var stock in availableStocks)
            {
                if (remainingToAllocate == 0) break;

                earmarked.TryGetValue(stock.Id, out var alreadyPlanned);
                var freeHere = stock.AvailableQuantity - alreadyPlanned;
                if (freeHere <= 0) continue;

                var quantityToTake = Math.Min(remainingToAllocate, freeHere);

                earmarked[stock.Id] = alreadyPlanned + quantityToTake;
                remainingToAllocate -= quantityToTake;

                plannedPicks.Add((stock, item.ProductId, stock.LocationId, stock.Location!.ZoneCode, quantityToTake));
            }

            // ------------------------------------------------------------
            // Phase 2: shortage handling. A shortage is a normal business
            // outcome, not an exception: park the order and report back.
            // ------------------------------------------------------------
            if (remainingToAllocate > 0)
            {
                order.Status = OrderStatus.AwaitingReplenishment;
                await _context.SaveChangesAsync();

                return (false, $"Shortage detected. Missing {remainingToAllocate} units for ProductId {item.ProductId}. Order parked.");
            }
        }

        // ----------------------------------------------------------------
        // Phase 3: allocation. Every item is covered, so commit the plan.
        // ----------------------------------------------------------------
        foreach (var pick in plannedPicks)
        {
            pick.Stock.ReservedQuantity += pick.Quantity;
        }

        foreach (var zoneGroup in plannedPicks.GroupBy(p => p.ZoneCode))
        {
            var pickTask = new PickTask
            {
                OrderId = order.Id,
                Sector = zoneGroup.Key,
                Status = PickTaskStatus.New,
                Items = zoneGroup.Select(p => new PickTaskItem
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

        return (true, null);
    }
}
