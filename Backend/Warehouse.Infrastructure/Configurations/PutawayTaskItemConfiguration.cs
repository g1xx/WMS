using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class PutawayTaskItemConfiguration : IEntityTypeConfiguration<PutawayTaskItem>
{
    public void Configure(EntityTypeBuilder<PutawayTaskItem> builder)
    {
        builder.HasOne(i => i.PutawayTask)
               .WithMany(t => t.Items)
               .HasForeignKey(i => i.PutawayTaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
               .WithMany()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
