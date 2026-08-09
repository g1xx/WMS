using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IStockTransactionRepository
{
    void Add(StockTransaction transaction);
}
