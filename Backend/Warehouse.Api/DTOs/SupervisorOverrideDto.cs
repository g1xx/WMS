namespace Warehouse.Api.DTOs
{
    public class SupervisorOverrideDto
    {
        // The supervisor's badge, scanned on the worker's device — encodes their user Id.
        public string BadgeBarcode { get; set; } = string.Empty;
    }
}
