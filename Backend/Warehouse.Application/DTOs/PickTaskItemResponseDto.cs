namespace Warehouse.Application.DTOs;


public class PickTaskItemResponseDto
{
    public Guid Id { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public int AvailableStock { get; set; }
}