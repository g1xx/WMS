using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(l => l.AddressBarcode).HasMaxLength(100);
        builder.Property(l => l.AssignedWorkerId).HasMaxLength(100);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Location_MaxDistinctSkus_PositiveOrNull",
            "\"MaxDistinctSkus\" IS NULL OR \"MaxDistinctSkus\" > 0"));

        // One transit location per worker. This is not just tidiness — it's what makes
        // the get-or-create in RelocationService safe: two concurrent first-uses by the
        // same worker race, and the loser's INSERT is rejected here rather than quietly
        // producing a second transit location that half their carried stock ends up in.
        // Filtered so the column stays free for every physical location, which leaves it
        // null. Same pattern as PickTaskConfiguration's ContainerId index.
        builder.HasIndex(l => l.AssignedWorkerId)
               .IsUnique()
               .HasFilter($"\"Type\" = {(int)LocationType.Transit}");

        builder.HasData(
            new Location
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                AddressBarcode = "tgn1",
                Floor = 1,
                Sector = "Rampa",
                WarehouseCode = "MAIN",
                Type = LocationType.DockDoor
            },
            new Location
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                AddressBarcode = "tgn2",
                Floor = 2,
                Sector = "Rampa",
                WarehouseCode = "MAIN",
                Type = LocationType.DockDoor
            },
            new Location
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                AddressBarcode = "tgn3",
                Floor = 3,
                Sector = "Rampa",
                WarehouseCode = "MAIN",
                Type = LocationType.DockDoor
            },
            new Location
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                AddressBarcode = "tgn4",
                Floor = 4,
                Sector = "Rampa",
                WarehouseCode = "MAIN",
                Type = LocationType.DockDoor
            },
            new Location
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000005"),
                AddressBarcode = "HZA301",
                Floor = 3,
                Sector = "ConveyorDrop",
                WarehouseCode = "MAIN",
                Type = LocationType.ConveyorDrop
            },
            new Location
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000006"),
                AddressBarcode = "HZA302",
                Floor = 3,
                Sector = "ConveyorDrop",
                WarehouseCode = "MAIN",
                Type = LocationType.ConveyorDrop
            },
            new Location
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000007"),
                AddressBarcode = "HZA303",
                Floor = 3,
                Sector = "ConveyorDrop",
                WarehouseCode = "MAIN",
                Type = LocationType.ConveyorDrop
            },
            new Location
            {
                Id = Guid.Parse("80000000-0000-0000-0000-000000000008"),
                AddressBarcode = "HZA304",
                Floor = 3,
                Sector = "ConveyorDrop",
                WarehouseCode = "MAIN",
                Type = LocationType.ConveyorDrop
            }
        );
    }
}