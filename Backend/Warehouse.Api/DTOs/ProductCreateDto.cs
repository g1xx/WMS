namespace Warehouse.Api.DTOs;

public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }

    // 0 = piece , 1 = package
    public int BaseUnit { get; set; } = 0;
    public int ItemPerPackage { get; set; } = 1;
}