using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithItemsAsync(Guid id);
    Task<List<Order>> GetAllWithItemsAsync();

    void Add(Order order);
}
