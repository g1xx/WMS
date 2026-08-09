# Graph Report - WMS  (2026-08-04)

## Corpus Check
- 143 files · ~34,952 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 821 nodes · 1351 edges · 72 communities (42 shown, 30 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 18 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `59a7aa68`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PickTask API & Service
- Frontend NPM Dependencies
- Products & Stocks API
- Frontend Lint & Axios Client
- Orders API & Allocation
- Containers API & Mapping
- TS App Compiler Config
- TS Node Compiler Config
- Controller Namespace Hub
- Auth Controller & Login
- Locations Controller API
- Project Build Targets
- API Launch Settings
- EF Model Snapshot
- Order Service Layer
- PickTask Entity Mapping
- Product & Stock Entities
- TestOrderGenerator/package.json
- compilerOptions
- compilerOptions
- TestOrderGenerator/src/App.tsx
- Product
- DI Composition Root
- Migration: PiecePackageProduct
- Migration: InitialCreate
- Migration: ProductUpdatedAt
- Migration: ProductVolumetrics
- Migration: ProductVolumetrics1
- Migration: Locations & Containers
- Migration: Orders & OrderItems
- Migration: Orders Constraints
- Migration: PickTasks & Items
- Migration: PickTasks & Items 1
- Migration: Identity Tables
- Migration: Containers Stock
- Migration: Reserved/Available Qty
- Snapshot: InitialCreate
- AppDbContext
- Snapshot: PiecePackageProduct
- Snapshot: ProductVolumetrics
- Snapshot: ProductVolumetrics1
- Snapshot: Locations & Containers
- Snapshot: Orders & OrderItems
- Snapshot: Orders Constraints
- Snapshot: PickTasks & Items
- PickTaskItem
- Snapshot: Reserved/Available Qty
- .AllocateOrderAsync
- TS Project References
- React + TypeScript + Vite
- CLAUDE.md
- 20260624095509_AddIdentityTables.Designer.cs
- 20260703175827_AddContainersStock.Designer.cs
- TestOrderGenerator/tsconfig.json
- SplitAndCloseDto.cs
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- Add1
- 20260624095509_AddIdentityTables.Designer.cs
- LocationResponseDto.cs
- DispatchContainerDto.cs
- LoginDto.cs
- PickItemDto.cs
- RegisterDto.cs
- IdentityUser
- DateTime
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- 20260801201234_Add1.Designer.cs
- OrderAllocationService.cs
- Stock
- 20260804092556_AddPutawayTables.Designer.cs

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Api.DTOs` - 42 edges
2. `Warehouse.Infrastructure.Migrations` - 33 edges
3. `Warehouse.Domain` - 31 edges
4. `Warehouse.Infrastructure` - 29 edges
5. `AppDbContext` - 27 edges
6. `Result` - 23 edges
7. `Container` - 17 edges
8. `compilerOptions` - 17 edges
9. `compilerOptions` - 17 edges
10. `PutawayTaskResponseDto` - 15 edges

## Surprising Connections (you probably didn't know these)
- `ContainersController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ContainersController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `LocationsController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/LocationsController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `OrdersController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/OrdersController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `ProductsController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ProductsController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `StocksController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/StocksController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs

## Import Cycles
- None detected.

## Communities (72 total, 30 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.07
Nodes (30): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task, PickTaskController (+22 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.22
Nodes (9): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+1 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.20
Nodes (13): axiosClient, App(), extractErrorMessage(), randomInt(), CreatedOrder, LogEntry, LogStatus, OrderCreatePayload (+5 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.09
Nodes (23): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+15 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.28
Nodes (8): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.17
Nodes (8): SplitAndCloseDto, Warehouse.Api.Common, Warehouse.Api.DTOs, Warehouse.Infrastructure, Warehouse.Api.Controllers, Warehouse.Domain, Warehouse.Api.Services, Warehouse.Application.Services

### Community 9 - "Auth Controller & Login"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+4 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.21
Nodes (12): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+4 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.13
Nodes (17): Warehouse.Api, net10.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), Warehouse.Domain, net10.0, Microsoft.NET.Sdk, Warehouse.Infrastructure (+9 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 14 - "Order Service Layer"
Cohesion: 0.26
Nodes (6): Guid, Task, IOrderService, Guid, Task, OrderService

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.24
Nodes (7): LocationCreateDto, Guid, ICollection, Location, LocationType, EntityTypeBuilder, LocationConfiguration

### Community 17 - "TestOrderGenerator/package.json"
Cohesion: 0.07
Nodes (28): dependencies, axios, react, react-dom, devDependencies, @types/node, @types/react, @types/react-dom (+20 more)

### Community 18 - "compilerOptions"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 19 - "compilerOptions"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 20 - "TestOrderGenerator/src/App.tsx"
Cohesion: 0.09
Nodes (27): Result, ResultErrorType, ActionResult, Guid, HttpGet, HttpPost, IActionResult, Task (+19 more)

### Community 21 - "Product"
Cohesion: 0.25
Nodes (7): Guid, IdentityUser, ModelBuilder, AppDbContext, DbSet, IdentityDbContext, IdentityRole

### Community 22 - "DI Composition Root"
Cohesion: 0.33
Nodes (5): Guid, IsAllocated, Message, Task, OrderAllocationService

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.19
Nodes (7): Props, Props, Phase, Props, PutawayTask, PutawayTaskItem, react

### Community 30 - "Migration: Orders Constraints"
Cohesion: 0.22
Nodes (5): MigrationBuilder, AddProductUpdatedAt, MigrationBuilder, AddPiecePackageProduct, Migration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.12
Nodes (15): ActionResult, HttpPost, Task, InventoryController, Guid, AdjustStockDto, CreateProductWithLocationDto, Guid (+7 more)

### Community 37 - "AppDbContext"
Cohesion: 0.21
Nodes (5): logout(), Props, Props, PendingFlow, Screen

### Community 39 - "Snapshot: ProductVolumetrics"
Cohesion: 0.27
Nodes (5): Props, Props, Props, PickTask, PickTaskItem

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.32
Nodes (6): ActionResult, HttpGet, HttpPost, IEnumerable, Task, StocksController

### Community 41 - "Snapshot: Locations & Containers"
Cohesion: 0.27
Nodes (7): Guid, ICollection, Container, ContainerStatus, ContainerType, EntityTypeBuilder, ContainerConfiguration

### Community 43 - "Snapshot: Orders Constraints"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, PickTask, PickTaskStatus, EntityTypeBuilder, PickTaskConfiguration

### Community 44 - "Snapshot: PickTasks & Items"
Cohesion: 0.24
Nodes (8): DateTime, Guid, ICollection, PutawayTask, PutawayTaskStatus, EntityTypeBuilder, PutawayTaskConfiguration, IEntityTypeConfiguration

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.11
Nodes (10): ModelBuilder, RefactorLocationsAndAddContainers, ModelBuilder, ConfigureOrdersConstraints, ModelBuilder, AddPickTasksAndItems, ModelBuilder, AppDbContextModelSnapshot (+2 more)

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "20260703175827_AddContainersStock.Designer.cs"
Cohesion: 0.29
Nodes (5): Guid, List, ProductResponseDto, Guid, StockCreateDto

### Community 55 - "SplitAndCloseDto.cs"
Cohesion: 0.24
Nodes (8): DateTime, Guid, ICollection, Product, ProductSize, UnitType, Guid, Stock

### Community 57 - "20260801193412_AddOrderItemPendingReplenishment.Designer.cs"
Cohesion: 0.25
Nodes (5): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration, Warehouse.Infrastructure.Configurations

### Community 60 - "LocationResponseDto.cs"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 61 - "DispatchContainerDto.cs"
Cohesion: 0.32
Nodes (7): extractErrorMessage(), getAvailableQuantity(), inputStyle, InventoryAdmin(), labelStyle, Product, ProductStock

### Community 63 - "PickItemDto.cs"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 65 - "IdentityUser"
Cohesion: 0.38
Nodes (3): axiosClient, App(), Login()

## Knowledge Gaps
- **153 isolated node(s):** `SplitAndCloseDto`, `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser` (+148 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **30 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `RegisterDto.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `20260801201234_Add1.Designer.cs`, `Snapshot: PiecePackageProduct`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `Snapshot: Orders & OrderItems`, `EF Model Snapshot`, `Snapshot: Reserved/Available Qty`, `.AllocateOrderAsync`, `Product & Stock Entities`, `20260624095509_AddIdentityTables.Designer.cs`, `20260624095509_AddIdentityTables.Designer.cs`, `LoginDto.cs`?**
  _High betweenness centrality (0.182) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `.AllocateOrderAsync` to `EF Model Snapshot`, `Product & Stock Entities`, `Migration: InitialCreate`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: Orders & OrderItems`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `20260624095509_AddIdentityTables.Designer.cs`, `LoginDto.cs`, `RegisterDto.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `20260801201234_Add1.Designer.cs`, `OrderAllocationService.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Product` to `PickTask API & Service`, `Products & Stocks API`, `Orders API & Allocation`, `Containers API & Mapping`, `Controller Namespace Hub`, `Locations Controller API`, `Order Service Layer`, `PickTask Entity Mapping`, `TestOrderGenerator/src/App.tsx`, `DI Composition Root`, `Snapshot: InitialCreate`, `Snapshot: ProductVolumetrics1`, `Snapshot: Locations & Containers`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `SplitAndCloseDto.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `LocationResponseDto.cs`, `PickItemDto.cs`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **What connects `SplitAndCloseDto`, `$schema`, `commandName` to the rest of the system?**
  _153 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.0697980684811238 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Orders API & Allocation` be split into smaller, more focused modules?**
  _Cohesion score 0.09269162210338681 - nodes in this community are weakly interconnected._