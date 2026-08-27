namespace Warehouse.Application.DTOs;

// What an upstream ERP / marketplace feed needs to name a product on an order line, and
// nothing else. Deliberately narrower than ProductResponseDto, which the staff terminal
// uses: that one carries a per-location stock breakdown, and warehouse layout is none of
// an external system's business. Price is absent from both.
//
// Id, not Sku alone, because OrderItemCreateDto identifies products by Guid — a feed that
// only knew SKUs could not actually place the order it was reading the catalogue for.
public class OrderableProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Summed across every location, so the caller sees how much it may order without
    // learning where any of it sits. Available (physical minus reserved), never physical:
    // units already reserved for a pick task cannot be sold again.
    public int AvailableQuantity { get; set; }
}
