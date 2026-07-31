namespace Warehouse.Domain;

public enum ContainerType
{
    Tote,      // Zbiórkowy
    Palox,     // Palox 
    Pallet     // Paleta
}

public enum ContainerStatus
{
    Available,  // Wolny
    InProgress, // W trakcie (или InTransit)
    Ready       // Gotowy
}
public class Container
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public ContainerType Type { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.Available;

    public decimal MaxWeightCapacityKg { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

}