namespace Warehouse.Application.DTOs;

public class ContainerValidationDto
{
    public bool IsValid { get; set; }

    // The sector this container's putaway work is actually in — same as the
    // worker's current sector when IsValid is true, different when it's not.
    public string ContainerSector { get; set; } = string.Empty;

    public Guid? PutawayTaskId { get; set; }

    public string Message { get; set; } = string.Empty;
}
