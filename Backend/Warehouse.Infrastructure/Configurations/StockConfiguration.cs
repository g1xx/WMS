using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.UseXminAsConcurrencyToken();

        // Prevent duplicate stock rows for the same product/location under concurrent putaway.
        builder.HasIndex(s => new { s.ProductId, s.LocationId }).IsUnique();

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_Stock_PhysicalQuantity_NonNegative", "\"PhysicalQuantity\" >= 0");
            tb.HasCheckConstraint("CK_Stock_ReservedQuantity_NonNegative", "\"ReservedQuantity\" >= 0");
            tb.HasCheckConstraint("CK_Stock_ReservedNotExceedingPhysical", "\"ReservedQuantity\" <= \"PhysicalQuantity\"");
        });
    }
}
