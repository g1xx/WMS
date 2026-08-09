namespace Warehouse.Application.Interfaces;

public interface IUnitOfWork
{
    IPickTaskRepository PickTasks { get; }
    IPutawayTaskRepository PutawayTasks { get; }
    IStockRepository Stocks { get; }
    IOrderRepository Orders { get; }
    IContainerRepository Containers { get; }
    IProductRepository Products { get; }
    ILocationRepository Locations { get; }
    IStockTransactionRepository StockTransactions { get; }

    Task<int> SaveChangesAsync();

    // Explicit transaction control for the multi-step flows (DispatchContainerAsync,
    // ReportDefectAsync, CreateProductWithLocationAsync) that must commit or roll back
    // several repository operations together.
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
