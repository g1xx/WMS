namespace Warehouse.Application.DTOs;

public class ReportPutawayMissingDto
{
    // No LocationBarcode: PutawayTaskItem has no fixed destination, and a shortage
    // here means the goods never arrived anywhere in the first place.
    public string ProductSku { get; set; } = string.Empty;
    public int MissingQuantity { get; set; }
}
