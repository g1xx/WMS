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
        Task<PickTaskResponseDto?> GetNextTaskAsync(string userId, string sector);

        Task<Result<string>> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId);

        Task<Result<string>> PickItemAsync(Guid id, PickItemDto dto, string userId);

        Task<Result<DispatchContainerResultDto>> DispatchContainerAsync(Guid id, DispatchContainerDto dto, string userId);

        Task<Result<MessageResponseDto>> CancelPickTaskAsync(Guid id, string userId);

        Task<Result<MessageResponseDto>> ReportMissingItemAsync(Guid taskId, ReportMissingItemDto dto, string workerId);

        Task<Result<ReportDefectResultDto>> ReportDefectAsync(Guid taskId, ReportDefectDto dto, string workerId);
    }
}