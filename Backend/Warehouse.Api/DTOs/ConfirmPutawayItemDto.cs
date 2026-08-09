namespace Warehouse.Api.DTOs;

public class ConfirmPutawayItemDto
{
    public string LocationBarcode { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
