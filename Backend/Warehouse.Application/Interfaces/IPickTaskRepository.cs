using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IPickTaskRepository
{
    // Container, Items.Product, Items.Location.Stocks — matches the GetPickTasksAsync,
    // GetActiveTaskForUserAsync and GetNextTaskAsync projection shapes.
    Task<List<PickTask>> GetAllWithDetailsAsync();
    Task<PickTask?> GetActiveForUserAsync(string userId);
    Task<PickTask?> GetNextForSectorAsync(string sector);

    Task<PickTask?> GetByIdAsync(Guid id);

    // Items.Product, Items.Location — used by PickItemAsync, ReportMissingItemAsync, ReportDefectAsync.
    Task<PickTask?> GetByIdWithItemsAndProductLocationAsync(Guid id);

    // Items only — used by DispatchContainerAsync, CancelPickTaskAsync.
    Task<PickTask?> GetByIdWithItemsAsync(Guid id);

    // Container.Location included — feeds OrdersController.PackOrder's readiness check.
    Task<List<PickTask>> GetByOrderIdWithContainerLocationAsync(Guid orderId);

    void Add(PickTask task);
}
