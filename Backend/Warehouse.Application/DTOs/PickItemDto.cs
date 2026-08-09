namespace Warehouse.Application.DTOs;

public class PickItemDto
{
    public string WorkerId { get; set; } = string.Empty;
    public string LocationBarcode { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}