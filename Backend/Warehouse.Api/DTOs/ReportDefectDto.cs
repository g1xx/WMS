namespace Warehouse.Api.DTOs;

public class ReportDefectDto
{
    public string LocationBarcode { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int DefectiveQuantity { get; set; }
}
