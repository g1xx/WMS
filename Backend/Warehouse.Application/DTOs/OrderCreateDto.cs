namespace Warehouse.Application.DTOs;

public class OrderCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public List<OrderItemCreateDto> Items { get; set; } = new List<OrderItemCreateDto>();
}

public class OrderItemCreateDto
{
    public Guid ProductId { get; set; }
    public int RequiredQuantity { get; set; }
}