namespace Warehouse.Api.DTOs;

public class CreateProductWithLocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }

    // 0 = piece, 1 = package
    public int BaseUnit { get; set; } = 0;
    public int ItemPerPackage { get; set; } = 1;

    // Target bin/shelf: reused if it already exists (Sector/WarehouseCode/Floor must match),
    // created otherwise. WarehouseCode + Sector + Floor drive the picking ZoneCode, so they
    // are required even though the rest of the address (aisle/rack/level/position) is not.
    public string LocationBarcode { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int Floor { get; set; }

    public int InitialQuantity { get; set; }
}
