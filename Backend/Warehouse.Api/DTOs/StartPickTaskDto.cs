namespace Warehouse.Api.DTOs;

public class StartPickTaskDto
{
    public string ContainerBarcode { get; set; } = string.Empty;
    public string WorkerId { get; set; } = string.Empty;
}