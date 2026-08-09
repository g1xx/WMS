using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(o => o.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(o => o.DestinationAddress).HasMaxLength(250).IsRequired();

        // (One-to-Many)
        // (Order)-(Items)
        builder.HasMany(o => o.Items)
               .WithOne(i => i.Order)
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.UseXminAsConcurrencyToken();
    }
}