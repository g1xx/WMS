using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IPutawayTaskRepository
{
    // Container, Items.Product, Items.DestinationLocation — the shape shared by
    // CreatePutawayTaskAsync's re-fetch, GetActivePutawayTaskForUserAsync,
    // StartPutawayForContainerAsync, ConfirmItemAsync and ReportMissingAsync.
    Task<PutawayTask?> GetByIdWithDetailsAsync(Guid id);
    Task<PutawayTask?> GetActiveForUserAsync(string workerId);
    Task<PutawayTask?> GetInProgressForContainerSectorWorkerAsync(Guid containerId, string sector, string workerId);
    Task<PutawayTask?> GetNewForContainerSectorAsync(Guid containerId, string sector);

    // No item/product includes needed — ValidateContainerAsync only reads Status/Sector.
    Task<List<PutawayTask>> GetPendingForContainerAsync(Guid containerId);

    // Used by the container-release check: are there other tasks for this container
    // that haven't reached a terminal state yet?
    Task<bool> HasOtherActiveTasksForContainerAsync(Guid containerId, Guid excludeTaskId);

    void Add(PutawayTask task);
}
