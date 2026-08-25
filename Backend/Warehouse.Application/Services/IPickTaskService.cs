using Warehouse.Application.Common;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Services
{
    public interface IPickTaskService
    {
        Task<IEnumerable<PickTaskResponseDto>> GetPickTasksAsync();

        // The worker's own in-flight task, independent of sector — used to
        // resume work after a re-login, before any sector has been chosen.
        Task<PickTaskResponseDto?> GetActiveTaskForUserAsync(string userId);

        // Next unassigned New task, strictly scoped to the given picking zone/sector.
        // CLAIMS the task for userId as it returns it — the same task is never handed to
        // two workers. Also sweeps claims that expired without a container scan.
        Task<PickTaskResponseDto?> GetNextTaskAsync(string userId, string sector);

        // Hands a claimed-but-not-started task back to the queue when the worker leaves
        // picking. No-ops if the task was already started or re-claimed by someone else.
        Task<Result<MessageResponseDto>> ReleasePickTaskAsync(Guid id, string userId);

        Task<Result<string>> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId);

        Task<Result<string>> PickItemAsync(Guid id, PickItemDto dto, string userId);

        Task<Result<DispatchContainerResultDto>> DispatchContainerAsync(Guid id, DispatchContainerDto dto, string userId);

        Task<Result<MessageResponseDto>> CancelPickTaskAsync(Guid id, string userId);

        Task<Result<MessageResponseDto>> ReportMissingItemAsync(Guid taskId, ReportMissingItemDto dto, string workerId);

        Task<Result<ReportDefectResultDto>> ReportDefectAsync(Guid taskId, ReportDefectDto dto, string workerId);
    }
}