using Warehouse.Application.Common;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Services;

public interface IPutawayService
{
    // Registers a container's expected inbound contents. Stands in for a
    // receiving/inbound flow this system doesn't have yet.
    Task<Result<PutawayTaskResponseDto>> CreatePutawayTaskAsync(CreatePutawayTaskDto dto);

    // The worker's own in-flight putaway task, independent of sector — mirrors
    // GetActiveTaskForUserAsync on the picking side, for resume-on-relogin.
    Task<PutawayTaskResponseDto?> GetActivePutawayTaskForUserAsync(string workerId);

    // Read-only: does this container have pending putaway work, and does it
    // match the worker's current sector? A sector mismatch is a normal business
    // outcome (IsValid = false), not a failure — only a genuinely missing/exhausted
    // container is a Result failure.
    Task<Result<ContainerValidationDto>> ValidateContainerAsync(string containerBarcode, string sector);

    // Claims (or resumes) the New/InProgress putaway task for this container in
    // this sector and returns its full item list.
    Task<Result<PutawayTaskResponseDto>> StartPutawayForContainerAsync(string containerBarcode, string sector, string workerId);

    Task<Result<PutawayTaskResponseDto>> ConfirmItemAsync(Guid taskId, ConfirmPutawayItemDto dto, string workerId);

    Task<Result<PutawayTaskResponseDto>> ReportMissingAsync(Guid taskId, ReportPutawayMissingDto dto, string workerId);
}
