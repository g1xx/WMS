using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class PutawayTaskConfiguration : IEntityTypeConfiguration<PutawayTask>
{
    public void Configure(EntityTypeBuilder<PutawayTask> builder)
    {
        builder.Property(t => t.Sector).HasMaxLength(50).IsRequired();
        builder.Property(t => t.AssignedWorkerId).HasMaxLength(100);

        builder.HasOne(t => t.Container)
               .WithMany()
               .HasForeignKey(t => t.ContainerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.UseXminAsConcurrencyToken();
    }
}
