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
        Ramp,

        // What a worker is physically carrying, as a location. One per worker (see
        // Location.AssignedWorkerId), created on first use. Relocation is then just two
        // ordinary stock movements — source -> transit, transit -> target — so Stock stays
        // the single source of truth for where every unit is, StockTransactions records
        // both legs for free, and every existing check and lock applies unchanged.
        //
        // Persisted as int with no HasConversion, so this MUST stay last: inserting a
        // member above it renumbers the ones below and silently reinterprets every
        // existing row. Same hazard as ContainerStatus.
        Transit
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

        // Which worker this location belongs to. Null for every physical location, set
        // only on Type == Transit. A plain user-id string rather than a foreign key,
        // matching PickTask.AssignedWorkerId exactly — the Identity tables aren't an FK
        // target in this model. Uniqueness (one transit location per worker) is enforced
        // by a filtered unique index, see LocationConfiguration.
        [MaxLength(100)]
        public string? AssignedWorkerId { get; set; }

        // Picking zone identity used to segregate PickTasks and route replacement
        // stock (e.g. "mp1" = WarehouseCode "m" + Sector "p" + Floor 1). Computed,
        // never persisted — always derived from the fields above.
        [NotMapped]
        public string ZoneCode => $"{WarehouseCode}{Sector}{Floor}";
    }
}
