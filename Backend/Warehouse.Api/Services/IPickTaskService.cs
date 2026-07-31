using Warehouse.Api.DTOs;

namespace Warehouse.Application.Services
{
    public interface IPickTaskService
    {
        Task<IEnumerable<PickTaskResponseDto>> GetPickTasksAsync();

        Task<PickTaskResponseDto?> GetNextTaskAsync(string userId);

        Task<string> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId);

        Task<string> PickItemAsync(Guid id, PickItemDto dto, string userId);
      
        Task<Guid?> DispatchContainerAsync(Guid id, DispatchContainerDto dto, string userId);

        Task<string> CancelPickTaskAsync(Guid id, string userId);

        Task<string> ReportMissingItemAsync(Guid taskId, ReportMissingItemDto dto, string workerId);
    }
}