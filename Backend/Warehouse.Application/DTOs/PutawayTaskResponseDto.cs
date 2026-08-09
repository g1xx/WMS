namespace Warehouse.Application.DTOs;

public class PutawayTaskItemResponseDto
{
    public Guid Id { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int PutAwayQuantity { get; set; }
    public int MissingQuantity { get; set; }
}

public class PutawayTaskResponseDto
{
    public Guid Id { get; set; }
    public string ContainerBarcode { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PutawayTaskItemResponseDto> Items { get; set; } = new();
}
