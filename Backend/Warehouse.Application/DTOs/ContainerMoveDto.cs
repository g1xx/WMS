namespace Warehouse.Application.DTOs;

public class ContainerMoveDto
{
    public string ContainerBarcode { get; set; } = string.Empty;

    public string DestinationLocationBarcode { get; set; } = string.Empty;
}