namespace Warehouse.Application.DTOs;

public class PutawayTaskItemResponseDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int PutAwayQuantity { get; set; }
    public int MissingQuantity { get; set; }

    // Address barcodes of locations where this product is already physically
    // stocked — a suggestion for the worker, not a restriction on where it can go.
    public List<string> SuggestedLocationBarcodes { get; set; } = new();
}

public class PutawayTaskResponseDto
{
    public Guid Id { get; set; }
    public string ContainerBarcode { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PutawayTaskItemResponseDto> Items { get; set; } = new();
}
