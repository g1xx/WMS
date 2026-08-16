using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.HasOne(c => c.Location)
               .WithMany()
               .HasForeignKey(c => c.LocationId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.AssignedSector).HasMaxLength(50);

        builder.HasIndex(c => c.Barcode).IsUnique();

        builder.UseXminAsConcurrencyToken();
    }
}