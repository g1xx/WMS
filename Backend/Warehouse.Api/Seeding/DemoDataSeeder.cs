using Microsoft.AspNetCore.Identity;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Api.Seeding;

// Opt-in demo-data seeder for a portfolio deployment — never runs on a normal
// `docker compose up` / `dotnet Warehouse.Api.dll` startup. Triggered explicitly via
// a CLI flag (see Program.cs: `dotnet Warehouse.Api.dll --seed-demo-data`), which lets
// it run as a one-off `docker compose exec` against an already-running container
// without needing a separate image or entrypoint. Kestrel never binds a port for this
// path — Program.cs returns before app.Run() when the flag is present.
//
// Every step checks for existing data before writing, so running this twice (or
// against a partially-seeded database) is safe. It deliberately does NOT touch
// LocationConfiguration.cs's HasData rows (the 4 dock doors + 4 conveyor drops) —
// it reuses one of the existing conveyor locations for the dispatch step below,
// rather than introducing a second source of truth for the same kind of data.
//
// The scripted order (ORD-DEMO-SHORTSHIP) is walked through the real
// OrderAllocationService/PickTaskService calls — the same code path the UI drives —
// rather than hand-crafted rows, so the resulting state (ShortedQuantity,
// IsPendingReplenishment, StockTransaction audit rows, Order.Status) is guaranteed
// consistent with everything those services actually enforce.
public class DemoDataSeeder
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "AdminDemo123!";

    // Reuses one of LocationConfiguration.cs's existing HasData conveyor drops —
    // see the class comment above for why this isn't seeded again here.
    private const string ConveyorBarcode = "HZA301";

    // Zone "mp1" — one of the active picking zones IUnfulfillableUnitHandler searches
    // for replacements in, so a genuinely-missing unit here has nowhere to be quietly
    // found (no other stock exists for the shorted product anywhere), guaranteeing a
    // deterministic ShortShipped outcome rather than one that depends on what else
    // happens to be seeded.
    private const string DemoWarehouse = "m";
    private const string DemoSector = "p";
    private const int DemoFloor = 1;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderAllocationService _orderAllocationService;
    private readonly IPickTaskService _pickTaskService;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DemoDataSeeder(
        IUnitOfWork unitOfWork,
        IOrderAllocationService orderAllocationService,
        IPickTaskService pickTaskService,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _unitOfWork = unitOfWork;
        _orderAllocationService = orderAllocationService;
        _pickTaskService = pickTaskService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        var admin = await SeedAdminUserAsync();

        var conveyor = await _unitOfWork.Locations.GetByBarcodeAsync(ConveyorBarcode);
        if (conveyor == null)
        {
            throw new InvalidOperationException(
                $"Expected seeded conveyor location '{ConveyorBarcode}' (see LocationConfiguration.cs) was not found. " +
                "Has the database actually been migrated?");
        }

        var locationBarcodeBySku = await SeedShelfLocationsAsync();
        var productsBySku = await SeedProductsAsync();
        await SeedStockAsync(productsBySku, locationBarcodeBySku);
        await SeedContainersAsync();

        var existingOrders = await _unitOfWork.Orders.GetAllWithItemsAsync();
        await SeedShortShipScenarioAsync(admin, productsBySku, locationBarcodeBySku, existingOrders);
        await SeedLiveDemoOrderAsync(productsBySku, existingOrders);

        Console.WriteLine();
        Console.WriteLine("=== Demo data ready ===");
        Console.WriteLine($"Admin login — username: {AdminUsername}  password: {AdminPassword}");
        Console.WriteLine("(This user is in the Admin role, which satisfies every Brigadier-or-Admin-gated");
        Console.WriteLine(" action too, so it can act as both the picker and the supervisor badge scan.)");
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in new[] { RoleNames.Worker, RoleNames.Brigadier, RoleNames.Admin })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                Console.WriteLine($"Created role: {roleName}");
            }
        }
    }

    private async Task<IdentityUser<Guid>> SeedAdminUserAsync()
    {
        var admin = await _userManager.FindByNameAsync(AdminUsername);
        if (admin != null)
        {
            Console.WriteLine($"Admin user '{AdminUsername}' already exists (id {admin.Id}) — leaving it as-is.");
            return admin;
        }

        admin = new IdentityUser<Guid>
        {
            UserName = AdminUsername,
            Email = "admin@wms.local",
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(admin, AdminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(admin, RoleNames.Admin);
        Console.WriteLine($"Created admin user '{AdminUsername}' (id {admin.Id}).");
        return admin;
    }

    // 6 shelf locations in the same zone, one per demo product — enough for the
    // scripted scenario and the live-demo order without needing hundreds of rows
    // (contrast with LocationsController.SeedMassLocations(), which generates ~25,000
    // and isn't idempotent — not appropriate for a lightweight portfolio seed).
    private async Task<Dictionary<string, string>> SeedShelfLocationsAsync()
    {
        var barcodes = Enumerable.Range(1, 6)
            .Select(rack => $"{DemoWarehouse}{DemoSector}{DemoFloor}01{rack:D3}01a")
            .ToList();

        var existing = await _unitOfWork.Locations.GetByBarcodesAsync(barcodes);
        var toCreate = barcodes.Where(b => !existing.ContainsKey(b)).ToList();

        if (toCreate.Count > 0)
        {
            var newLocations = toCreate.Select(barcode => new Location
            {
                Type = LocationType.Shelf,
                WarehouseCode = DemoWarehouse,
                Sector = DemoSector,
                Floor = DemoFloor,
                Aisle = "01",
                Rack = barcode.Substring(5, 3),
                Level = "01",
                Position = "a",
                AddressBarcode = barcode,
            }).ToList();

            _unitOfWork.Locations.AddRange(newLocations);
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"Created {newLocations.Count} shelf location(s) in zone {DemoWarehouse}{DemoSector}{DemoFloor}.");
        }

        // One barcode per product SKU, assigned in the same order both are declared —
        // see SeedProductsAsync/SeedStockAsync, which zip these back together.
        return barcodes
            .Zip(DemoProductSkus, (barcode, sku) => (sku, barcode))
            .ToDictionary(x => x.sku, x => x.barcode);
    }

    private static readonly string[] DemoProductSkus =
        { "WM-100", "UC-200", "KB-300", "MN-400", "LS-500", "WC-600" };

    private async Task<Dictionary<string, Product>> SeedProductsAsync()
    {
        var candidates = new List<Product>
        {
            new() { Name = "Wireless Mouse", Sku = "WM-100", Price = 24.99m, WeightKg = 0.12m, LengthCm = 12, WidthCm = 6, HeightCm = 4, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
            new() { Name = "USB-C Cable 2m", Sku = "UC-200", Price = 9.99m, WeightKg = 0.08m, LengthCm = 20, WidthCm = 8, HeightCm = 2, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
            new() { Name = "Mechanical Keyboard", Sku = "KB-300", Price = 89.99m, WeightKg = 0.95m, LengthCm = 44, WidthCm = 14, HeightCm = 4, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
            new() { Name = "27-inch Monitor", Sku = "MN-400", Price = 249.99m, WeightKg = 5.2m, LengthCm = 62, WidthCm = 15, HeightCm = 45, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
            new() { Name = "Laptop Stand", Sku = "LS-500", Price = 34.99m, WeightKg = 1.1m, LengthCm = 25, WidthCm = 22, HeightCm = 15, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
            new() { Name = "Webcam 1080p", Sku = "WC-600", Price = 45.99m, WeightKg = 0.15m, LengthCm = 10, WidthCm = 5, HeightCm = 5, BaseUnit = UnitType.piece, ItemPerPackage = 1 },
        };

        var created = 0;
        foreach (var product in candidates)
        {
            if (await _unitOfWork.Products.SkuExistsAsync(product.Sku)) continue;
            _unitOfWork.Products.Add(product);
            created++;
        }
        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"Created {created} demo product(s).");
        }

        return await _unitOfWork.Products.GetBySkusAsync(candidates.Select(p => p.Sku).ToList());
    }

    private async Task SeedStockAsync(Dictionary<string, Product> productsBySku, Dictionary<string, string> locationBarcodeBySku)
    {
        var locations = await _unitOfWork.Locations.GetByBarcodesAsync(locationBarcodeBySku.Values.ToList());
        var created = 0;

        foreach (var (sku, product) in productsBySku)
        {
            var location = locations[locationBarcodeBySku[sku]];
            var existingStock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(product.Id, location.Id);
            if (existingStock != null) continue;

            _unitOfWork.Stocks.Add(new Stock
            {
                ProductId = product.Id,
                LocationId = location.Id,
                PhysicalQuantity = 50,
                ReservedQuantity = 0,
            });
            created++;
        }
        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"Created {created} stock row(s) (50 units each).");
        }
    }

    // "DEMO-CONT-1" is consumed by the scripted ShortShip scenario; the other two are
    // left free (New) for you to scan in the UI when walking the live demo order.
    private async Task SeedContainersAsync()
    {
        var barcodes = new[] { "DEMO-CONT-1", "DEMO-CONT-2", "DEMO-CONT-3" };
        var created = 0;

        foreach (var barcode in barcodes)
        {
            if (await _unitOfWork.Containers.ExistsByBarcodeAsync(barcode)) continue;

            _unitOfWork.Containers.Add(new Container
            {
                Barcode = barcode,
                Type = ContainerType.Tote,
                Status = ContainerStatus.New,
                MaxWeightCapacityKg = 20,
            });
            created++;
        }
        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"Created {created} container(s).");
        }
    }

    // Walks a real order through allocation, a partial pick, a supervisor-confirmed
    // missing-item write-off with no replacement available, and dispatch — landing on
    // OrderStatus.ShortShipped exactly as a worker + supervisor would produce it from
    // the UI. See the class comment for why this drives the real services instead of
    // inserting rows directly.
    private async Task SeedShortShipScenarioAsync(
        IdentityUser<Guid> admin,
        Dictionary<string, Product> productsBySku,
        Dictionary<string, string> locationBarcodeBySku,
        List<Order> existingOrders)
    {
        const string orderNumber = "ORD-DEMO-SHORTSHIP";
        if (existingOrders.Any(o => o.OrderNumber == orderNumber))
        {
            Console.WriteLine($"Demo order '{orderNumber}' already exists — skipping the scripted walkthrough.");
            return;
        }

        var mouse = productsBySku["WM-100"];
        var monitor = productsBySku["MN-400"];
        var adminId = admin.Id.ToString();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = "Portfolio Demo Customer",
            DestinationAddress = "123 Demo Street, Warehouse City",
            Status = OrderStatus.New,
            Items = new List<OrderItem>
            {
                new() { ProductId = mouse.Id, RequiredQuantity = 5 },
                new() { ProductId = monitor.Id, RequiredQuantity = 5 },
            },
        };
        _unitOfWork.Orders.Add(order);
        await _unitOfWork.SaveChangesAsync();

        var (allocated, allocationMessage) = await _orderAllocationService.AllocateOrderAsync(order.Id);
        if (!allocated)
            throw new InvalidOperationException($"Demo order allocation unexpectedly failed: {allocationMessage}");

        var pickTasks = await _unitOfWork.PickTasks.GetByOrderIdWithContainerLocationAsync(order.Id);
        var task = pickTasks.Single();

        var startResult = await _pickTaskService.StartPickTaskAsync(
            task.Id,
            new StartPickTaskDto { ContainerBarcode = "DEMO-CONT-1", WorkerId = adminId },
            adminId);
        ThrowIfFailed(startResult.IsSuccess, startResult.Error, "start the demo pick task");

        var mousePick = await _pickTaskService.PickItemAsync(
            task.Id,
            new PickItemDto { WorkerId = adminId, LocationBarcode = locationBarcodeBySku["WM-100"], ProductSku = "WM-100", Quantity = 5 },
            adminId);
        ThrowIfFailed(mousePick.IsSuccess, mousePick.Error, "pick the mouse (fully in stock)");

        // Partial pick: 3 of 5 found on the shelf.
        var monitorPick = await _pickTaskService.PickItemAsync(
            task.Id,
            new PickItemDto { WorkerId = adminId, LocationBarcode = locationBarcodeBySku["MN-400"], ProductSku = "MN-400", Quantity = 3 },
            adminId);
        ThrowIfFailed(monitorPick.IsSuccess, monitorPick.Error, "partially pick the monitor");

        // Supervisor override: the remaining 2 are confirmed genuinely missing. No other
        // stock exists for this SKU anywhere, so the replacement search comes up empty
        // and this becomes a real, unrecoverable shortfall (ShortedQuantity + IsPendingReplenishment).
        var missingReport = await _pickTaskService.ReportMissingItemAsync(
            task.Id,
            new ReportMissingItemDto { LocationBarcode = locationBarcodeBySku["MN-400"], ProductSku = "MN-400", MissingQuantity = 2 },
            adminId);
        ThrowIfFailed(missingReport.IsSuccess, missingReport.Error, "report the monitor shortfall (supervisor override)");

        var dispatchResult = await _pickTaskService.DispatchContainerAsync(
            task.Id,
            new DispatchContainerDto { ContainerBarcode = "DEMO-CONT-1", ConveyorBarcode = ConveyorBarcode },
            adminId);
        ThrowIfFailed(dispatchResult.IsSuccess, dispatchResult.Error, "dispatch the demo container");

        Console.WriteLine(
            $"Created and walked order '{orderNumber}' through: mouse fully picked (5/5), " +
            "monitor partially picked (3/5) + 2 confirmed missing with no replacement found -> " +
            "dispatched as ShortShipped.");
    }

    // Left New/unallocated on purpose — this one is for you to click through in the UI
    // yourself (Allocate -> Start Picking -> pick -> dispatch) to demo the clean path.
    private async Task SeedLiveDemoOrderAsync(Dictionary<string, Product> productsBySku, List<Order> existingOrders)
    {
        const string orderNumber = "ORD-DEMO-LIVE";
        if (existingOrders.Any(o => o.OrderNumber == orderNumber))
        {
            Console.WriteLine($"Demo order '{orderNumber}' already exists — leaving it as-is.");
            return;
        }

        var keyboard = productsBySku["KB-300"];
        var webcam = productsBySku["WC-600"];

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = "Live Demo Customer",
            DestinationAddress = "456 Demo Avenue, Warehouse City",
            Status = OrderStatus.New,
            Items = new List<OrderItem>
            {
                new() { ProductId = keyboard.Id, RequiredQuantity = 3 },
                new() { ProductId = webcam.Id, RequiredQuantity = 3 },
            },
        };
        _unitOfWork.Orders.Add(order);
        await _unitOfWork.SaveChangesAsync();

        Console.WriteLine($"Created order '{orderNumber}' (New, unallocated) for you to walk through live in the UI.");
    }

    private static void ThrowIfFailed(bool isSuccess, string? error, string action)
    {
        if (!isSuccess)
            throw new InvalidOperationException($"Demo seeding failed to {action}: {error}");
    }
}
