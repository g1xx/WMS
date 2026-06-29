using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections;

namespace Warehouse.Domain
{
    public enum ProductSize
    {
        S,
        M,
        L,
        XL,
        XXL 
    }
    public enum UnitType
    {
        piece,
        package
    }
    public class Product
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }

        public decimal WeightKg { get; set; }

        public decimal LengthCm { get; set; }
        public decimal WidthCm { get; set; }
        public decimal HeightCm { get; set; }
        [NotMapped]
        public ProductSize SizeCategory
        {
            get
            {
                if (LengthCm >= 120 || WidthCm >= 80) return ProductSize.XXL;

                var volume = LengthCm * WidthCm * HeightCm;

                if (volume < 5000) return ProductSize.S;     
                if (volume < 20000) return ProductSize.M;     
                if (volume < 50000) return ProductSize.L;     
                return ProductSize.XL;                         
            }
        }

        public string? ImageUrl { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public UnitType BaseUnit {  get; set; } = UnitType.piece;
        public int ItemPerPackage { get; set; } = 1;
        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    }
}
