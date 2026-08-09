using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Interfaces;
using Warehouse.Infrastructure.Repositories;

namespace Warehouse.Infrastructure;

// Composition root for this layer: everything that needs AppDbContext or EF Core
// directly lives behind this one extension method, so the Api project's Program.cs
// never has to reference either.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPickTaskRepository, PickTaskRepository>();
        services.AddScoped<IPutawayTaskRepository, PutawayTaskRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IContainerRepository, ContainerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();

        return services;
    }
}
