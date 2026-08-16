namespace Warehouse.Application.DTOs;

public class CreatePutawayTaskItemDto
{
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
}

// Registers a container's expected inbound contents for putaway. There is no
// receiving/inbound flow in this system yet, so this is the entry point that
// stands in for one — see PutawayService for the reasoning.
//
// The destination for each item is no longer fixed at creation time — the worker
// chooses it during execution (see ConfirmPutawayItemDto.LocationBarcode), so all
// this needs is which zone the task should be routed to.
public class CreatePutawayTaskDto
{
    public string ContainerBarcode { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public List<CreatePutawayTaskItemDto> Items { get; set; } = new();
}
