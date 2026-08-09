namespace Warehouse.Application.DTOs;

public class CreatePutawayTaskItemDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string DestinationLocationBarcode { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
}

// Registers a container's expected inbound contents for putaway. There is no
// receiving/inbound flow in this system yet, so this is the entry point that
// stands in for one — see PutawayService for the reasoning.
public class CreatePutawayTaskDto
{
    public string ContainerBarcode { get; set; } = string.Empty;
    public List<CreatePutawayTaskItemDto> Items { get; set; } = new();
}
