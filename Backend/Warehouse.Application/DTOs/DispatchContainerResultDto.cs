namespace Warehouse.Application.DTOs;

public class DispatchContainerResultDto
{
    public string Message { get; set; } = string.Empty;
    public Guid? NextTaskId { get; set; }
}
