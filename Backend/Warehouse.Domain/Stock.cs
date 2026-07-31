using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Warehouse.Domain
{
    public class Stock
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public Guid LocationId { get; set; }
        public Location? Location { get; set; }

        [Required]
        public int PhysicalQuantity { get; set; }

        [Required]
        public int ReservedQuantity { get; set; }

        [NotMapped] 
        public int AvailableQuantity => PhysicalQuantity - ReservedQuantity;
    }
}