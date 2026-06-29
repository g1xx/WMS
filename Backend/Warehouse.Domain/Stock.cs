using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Collections;

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
        public int Quantity { get; set; }
    }
}
