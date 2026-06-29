namespace Warehouse.Api.DTOs;

public class StockCreateDto
{
    public Guid ProductId { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;

    public int Quantity { get; set; }
}