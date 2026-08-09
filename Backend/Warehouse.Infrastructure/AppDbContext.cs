using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;

namespace Warehouse.Infrastructure
{
    public class AppDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
    {
        //constructor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //DbSet properties for each entity
        public DbSet<Product> Products { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Container> Containers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems{ get; set; }
        public DbSet<PickTask> PickTasks { get; set; }
        public DbSet<PickTaskItem> PickTaskItems { get; set; }
        public DbSet<PutawayTask> PutawayTasks { get; set; }
        public DbSet<PutawayTaskItem> PutawayTaskItems { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<Location>()
                .HasIndex(l => l.AddressBarcode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Stocks)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Location>()
                .HasMany(l => l.Stocks)
                .WithOne(s => s.Location)
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); // Infrastructure/Configurations
        }


    }
}
