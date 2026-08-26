using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using Warehouse.Api.Common;
using Warehouse.Api.Middleware;
using Warehouse.Api.Seeding;
using Warehouse.Application.Common;
using Warehouse.Application.Services;
using Warehouse.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// UseExceptionHandler()'s parameterless overload validates at startup that either
// ExceptionHandlingPath/ExceptionHandler is set OR an IProblemDetailsService is
// registered — it does NOT check whether an IExceptionHandler is registered, so
// AddProblemDetails() is required here even though GlobalExceptionHandler handles
// every exception itself and this fallback is never actually reached.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IOrderAllocationService, OrderAllocationService>();
// A container is a shared physical resource — its status IS the lock. This is the
// only service allowed to assign Container.Status after creation; see
// ContainerTransitions/ContainerLifecycleService.
builder.Services.AddScoped<IContainerLifecycleService, ContainerLifecycleService>();
// Bound here rather than injected as IOptions<T>: Warehouse.Application has no package
// references beyond Warehouse.Domain, so PickTaskSettings is a plain POCO and the binding
// lives in the one project that already has the configuration stack. Falls back to the
// POCO's own defaults when the section is absent.
builder.Services.AddSingleton(
    builder.Configuration.GetSection("PickTaskSettings").Get<PickTaskSettings>() ?? new PickTaskSettings());
// Absent section => Enabled stays false => the demo help endpoint 404s. See DemoSettings.
builder.Services.AddSingleton(
    builder.Configuration.GetSection("DemoSettings").Get<DemoSettings>() ?? new DemoSettings());
builder.Services.AddScoped<IPickTaskService, PickTaskService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
// The destination half of every stock movement (lock, MaxDistinctSkus, find-or-create,
// audit row). Shared by putaway and relocation so the capacity rule has one implementation.
builder.Services.AddScoped<IStockPlacementService, StockPlacementService>();
builder.Services.AddScoped<IPutawayService, PutawayService>();
builder.Services.AddScoped<IRelocationService, RelocationService>();
builder.Services.AddSingleton<IRouteOptimizerService, RouteOptimizerService>();
builder.Services.AddSingleton<IDefectReplacementPlanner, DefectReplacementPlanner>();
// Scoped, not Singleton: it depends on IUnitOfWork (tied to the per-request DbContext).
builder.Services.AddScoped<IUnfulfillableUnitHandler, UnfulfillableUnitHandler>();
// Only ever resolved by the --seed-demo-data path below, never during normal requests.
builder.Services.AddScoped<DemoDataSeeder>();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter the JWT token like this: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; 
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// http://localhost:5173 / :5175 are the Vite dev servers (warehouse-client,
// TestOrderGenerator); :80 / :3000 are the nginx-served Docker frontend (see
// Frontend/warehouse-client/Dockerfile — port depends on the compose port mapping).
//
// The deployed frontend itself doesn't actually need an entry here: nginx proxies
// /api same-origin (see nginx.conf), so the browser never makes a cross-origin
// call to reach it. These origins stay for anything that DOES call the API
// directly (TestOrderGenerator, Swagger "Try it out" from a different origin,
// manual testing). https:// is listed now, ahead of actually enabling it, so this
// file doesn't need touching again once a TLS-terminating layer is added in front.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173", "http://localhost:5175",
                  "http://localhost", "http://localhost:80", "http://localhost:3000",
                  "http://wms.polandcentral.cloudapp.azure.com",
                  "https://wms.polandcentral.cloudapp.azure.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Must wrap everything downstream, so it stays first in the pipeline.
app.UseExceptionHandler();

// Applies any pending migrations on startup so a fresh container (or a fresh
// `docker compose up`) doesn't need a manual `dotnet ef database update` step —
// the db service's healthcheck (see docker-compose.yml) ensures Postgres is
// actually ready to accept connections before this runs.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Opt-in only — never runs on a normal startup. Trigger with:
//   docker compose exec backend dotnet Warehouse.Api.dll --seed-demo-data
// Exits immediately after, before Kestrel ever binds a port (that happens inside
// app.Run() below), so it can't collide with the already-running container. See
// Warehouse.Api/Seeding/DemoDataSeeder.cs for what it creates.
if (args.Contains("--seed-demo-data"))
{
    using var seedScope = app.Services.CreateScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();