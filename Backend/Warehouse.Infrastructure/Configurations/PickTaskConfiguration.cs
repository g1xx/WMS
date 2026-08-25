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

        // Serves both halves of the claim transaction (see PickTaskRepository):
        // the expiry sweep and the SELECT ... FOR UPDATE SKIP LOCKED that follows it.
        // Both filter on Sector + Status = New and order by CreatedAt, and both run on
        // every "next task" request, so this is the hot path for every picking terminal.
        builder.HasIndex(t => new { t.Sector, t.Status, t.CreatedAt });
    }
}