using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasOne(i => i.Product)
               .WithMany() 
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.RequiredQuantity).IsRequired();
        builder.Property(i => i.PickedQuantity).IsRequired();
        builder.Property(i => i.ShortedQuantity).IsRequired();

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_OrderItem_ShortedQuantity_NonNegative", "\"ShortedQuantity\" >= 0");
        });
    }
}