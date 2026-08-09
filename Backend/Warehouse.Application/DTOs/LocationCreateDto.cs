using System;
using System.ComponentModel.DataAnnotations;
using Warehouse.Domain;

namespace Warehouse.Application.DTOs
{
    public class LocationCreateDto
    {
        public LocationType Type { get; set; } = LocationType.Shelf;

        [Required]
        [MaxLength(50)]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Sector { get; set; } = string.Empty;

        public int Floor { get; set; }

        [Required]
        public string Aisle { get; set; } = string.Empty;

        [Required]
        public string Rack { get; set; } = string.Empty;

        [Required]
        public string Level { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;
    }

   
}