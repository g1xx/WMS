using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain;

namespace Warehouse.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(l => l.AddressBarcode).HasMaxLength(100);

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