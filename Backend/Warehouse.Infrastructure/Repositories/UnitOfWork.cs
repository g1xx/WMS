using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Application.Common;
using Warehouse.Application.Interfaces;

namespace Warehouse.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IPickTaskRepository PickTasks { get; }
    public IPutawayTaskRepository PutawayTasks { get; }
    public IStockRepository Stocks { get; }
    public IOrderRepository Orders { get; }
    public IContainerRepository Containers { get; }
    public IProductRepository Products { get; }
    public ILocationRepository Locations { get; }
    public IStockTransactionRepository StockTransactions { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;

        PickTasks = new PickTaskRepository(context);
        PutawayTasks = new PutawayTaskRepository(context);
        Stocks = new StockRepository(context);
        Orders = new OrderRepository(context);
        Containers = new ContainerRepository(context);
        Products = new ProductRepository(context);
        Locations = new LocationRepository(context);
        StockTransactions = new StockTransactionRepository(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The record was modified by another process before this change could be saved.", ex);
        }
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null) return;

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null) return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await BeginTransactionAsync();
        try
        {
            await action();
            await CommitTransactionAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        await BeginTransactionAsync();
        try
        {
            var result = await action();
            await CommitTransactionAsync();
            return result;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }
}
