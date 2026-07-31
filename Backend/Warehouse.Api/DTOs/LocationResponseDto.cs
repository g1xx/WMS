using System;
using System.ComponentModel.DataAnnotations;
using Warehouse.Domain;

namespace Warehouse.Api.DTOs
{
    public class LocationResponseDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string AddressBarcode { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string Aisle { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }
}
