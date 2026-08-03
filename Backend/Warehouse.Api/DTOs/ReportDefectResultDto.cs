namespace Warehouse.Api.DTOs;

public class ReportDefectResultDto
{
    public int DefectiveQuantityDeducted { get; set; }

    // Replacement units appended to the worker's current PickTask (same zone)
    public int AppendedToCurrentTaskQuantity { get; set; }

    // One entry per new PickTask created in a different picking zone
    public List<Guid> NewPickTaskIds { get; set; } = new();

    // Units that could not be sourced from any standard picking zone
    // (only bulk/high-rack stock left, or none at all) — marked pending replenishment
    public int ShortageQuantity { get; set; }

    public string Message { get; set; } = string.Empty;
}
