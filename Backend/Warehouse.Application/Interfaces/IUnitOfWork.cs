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

    // Explicit transaction control, for callers that need to interleave it with
    // steps ExecuteInTransactionAsync can't express (e.g. an early rollback-and-
    // return before any mutation has happened).
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();

    // Runs action() inside Begin/Commit, rolling back and rethrowing on any
    // exception — the multi-step flows (DispatchContainerAsync, ReportDefectAsync,
    // ConfirmItemAsync, CreateProductWithLocationAsync) all had this exact
    // try/catch/rollback/throw hand-written identically; this is the one copy.
    Task ExecuteInTransactionAsync(Func<Task> action);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
}
