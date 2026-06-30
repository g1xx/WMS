namespace Warehouse.Api.DTOs;

public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string SizeCategory { get; set; } = string.Empty;
    public List<StockCreateDto> Stocks { get; set; } = new List<StockCreateDto>();
}

