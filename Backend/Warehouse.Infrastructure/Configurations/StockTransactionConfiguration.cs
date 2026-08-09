using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.Property(t => t.UserId).HasMaxLength(100).IsRequired();

        builder.HasOne(t => t.Product)
               .WithMany()
               .HasForeignKey(t => t.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Location)
               .WithMany()
               .HasForeignKey(t => t.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Timestamp);
        builder.HasIndex(t => new { t.ProductId, t.LocationId });
    }
}
