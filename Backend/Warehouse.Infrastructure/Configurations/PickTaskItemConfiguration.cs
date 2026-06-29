using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class PickTaskItemConfiguration : IEntityTypeConfiguration<PickTaskItem>
{
    public void Configure(EntityTypeBuilder<PickTaskItem> builder)
    {
        builder.HasOne(i => i.PickTask)
               .WithMany(t => t.Items)
               .HasForeignKey(i => i.PickTaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
               .WithMany()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Location)
               .WithMany()
               .HasForeignKey(i => i.LocationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}