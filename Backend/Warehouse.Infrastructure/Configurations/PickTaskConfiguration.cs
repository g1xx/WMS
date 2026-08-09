using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class PickTaskConfiguration : IEntityTypeConfiguration<PickTask>
{
    public void Configure(EntityTypeBuilder<PickTask> builder)
    {
        builder.Property(t => t.Sector).HasMaxLength(50).IsRequired();
        builder.Property(t => t.AssignedWorkerId).HasMaxLength(100);

        builder.HasOne(t => t.Order)
               .WithMany()
               .HasForeignKey(t => t.OrderId)
               .OnDelete(DeleteBehavior.Cascade); 

        builder.HasOne(t => t.Container)
               .WithMany()
               .HasForeignKey(t => t.ContainerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.UseXminAsConcurrencyToken();

        // One container can only have one active pick task at a time.
        builder.HasIndex(t => t.ContainerId)
               .IsUnique()
               .HasFilter($"\"Status\" = {(int)PickTaskStatus.InProgress}");
    }
}