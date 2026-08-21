using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;

namespace Warehouse.Domain
{
    public enum LocationType
    {
        Shelf,         
        FloorZone,     
        DockDoor,      
        ConveyorDrop,
        Ramp
    }
    public class Location
    {
        [Key]
        public Guid Id { get; set; }
        public LocationType Type { get; set; } = LocationType.Shelf;

        [MaxLength(100)]
        public string AddressBarcode { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string Aisle { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

        // Null means "use LocationCapacityDefaults for this Type" — most locations
        // never need a per-row override, only LocationType-level defaults. Null also
        // means "no limit" once resolved (for staging types), never zero.
        public int? MaxDistinctSkus { get; set; }

        // Picking zone identity used to segregate PickTasks and route replacement
        // stock (e.g. "mp1" = WarehouseCode "m" + Sector "p" + Floor 1). Computed,
        // never persisted — always derived from the fields above.
        [NotMapped]
        public string ZoneCode => $"{WarehouseCode}{Sector}{Floor}";
    }
}
