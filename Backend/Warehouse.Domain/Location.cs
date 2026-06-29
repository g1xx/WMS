using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
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
    }
}
