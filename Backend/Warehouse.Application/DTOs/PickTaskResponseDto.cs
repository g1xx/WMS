namespace Warehouse.Application.DTOs
{
    public class PickTaskResponseDto
    {
        public Guid Id { get; set; }
        public string Sector { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ContainerBarcode { get; set; }
        public string? AssignedWorkerId { get; set; }
        public List<PickTaskItemResponseDto> Items { get; set; } = new();
    }

}