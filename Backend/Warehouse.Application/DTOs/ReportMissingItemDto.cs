namespace Warehouse.Application.DTOs
{
    public class ReportMissingItemDto
    {
        public string LocationBarcode { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public int MissingQuantity { get; set; } // How many units could not be found
    }
}