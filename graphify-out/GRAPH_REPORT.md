# Graph Report - WMS  (2026-08-09)

## Corpus Check
- 160 files · ~41,188 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 901 nodes · 1491 edges · 77 communities (38 shown, 39 thin omitted)
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
- Container
- PickTask
- PutawayTask
- AppDbContext
- PickTaskItem
- Snapshot: Reserved/Available Qty
- .AllocateOrderAsync
- TS Project References
- React + TypeScript + Vite
- CLAUDE.md
- 20260624095509_AddIdentityTables.Designer.cs
- Warehouse.Api.Common
- TestOrderGenerator/tsconfig.json
- PickTaskItem
- Product
- Add1
- 20260624095509_AddIdentityTables.Designer.cs
- PutawayTaskItem
- Stock
- AddReservedAvailableQuantity
- AddConcurrencyAndStockConstraints
- RegisterDto.cs
- 20260618193047_AddProductVolumetrics.Designer.cs
- DateTime
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- 20260801201234_Add1.Designer.cs
- OrderAllocationService.cs
- Stock
- 20260804092556_AddPutawayTables.Designer.cs
- AddStockTransactionJournal
- 20260611180526_InitialCreate.Designer.cs
- 20260615013749_AddPiecePackageProduct.Designer.cs
- 20260805012255_AddStockTransactionJournal.Designer.cs
- PickTaskItemResponseDto

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Api.DTOs` - 45 edges
2. `Warehouse.Infrastructure.Migrations` - 39 edges
3. `Warehouse.Domain` - 34 edges
4. `Warehouse.Infrastructure` - 32 edges
5. `Result` - 31 edges
6. `AppDbContext` - 28 edges
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

## Communities (77 total, 39 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.06
Nodes (34): Result, ResultErrorType, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult (+26 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.05
Nodes (36): ActionResult, HttpPost, Task, InventoryController, ActionResult, DateTime, Guid, HttpGet (+28 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.13
Nodes (23): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+15 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.07
Nodes (28): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+20 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.15
Nodes (15): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto (+7 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.22
Nodes (4): SplitAndCloseDto, Warehouse.Api.DTOs, Warehouse.Api.Controllers, Warehouse.Domain

### Community 9 - "Auth Controller & Login"
Cohesion: 0.15
Nodes (14): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+6 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.23
Nodes (11): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+3 more)

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
Cohesion: 0.28
Nodes (7): LocationCreateDto, Guid, ICollection, Location, LocationType, EntityTypeBuilder, LocationConfiguration

### Community 16 - "Product & Stock Entities"
Cohesion: 0.14
Nodes (7): ModelBuilder, AddProductUpdatedAt, ModelBuilder, AddProductVolumetrics, ModelBuilder, AddPickTasksAndItems, Warehouse.Infrastructure.Migrations

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
Cohesion: 0.08
Nodes (27): AllowAnonymous, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, Task (+19 more)

### Community 21 - "Product"
Cohesion: 0.50
Nodes (3): MigrationBuilder, AddProductUpdatedAt, Migration

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.06
Nodes (33): Mode, axios, axiosClient, AxiosRequestConfig, fetchSupervisorAuthHeader(), isSupervisorAuthError(), logout(), App() (+25 more)

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.29
Nodes (3): EntityTypeBuilder, XminConcurrencyExtensions, Warehouse.Infrastructure.Configurations

### Community 41 - "Container"
Cohesion: 0.28
Nodes (7): DateTime, Guid, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration, IEntityTypeConfiguration

### Community 42 - "PickTask"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, PickTask, PickTaskStatus, EntityTypeBuilder, PickTaskConfiguration

### Community 43 - "PutawayTask"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, PutawayTask, PutawayTaskStatus, EntityTypeBuilder, PutawayTaskConfiguration

### Community 44 - "AppDbContext"
Cohesion: 0.25
Nodes (7): Guid, IdentityUser, ModelBuilder, AppDbContext, DbSet, IdentityDbContext, IdentityRole

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.40
Nodes (3): ModelBuilder, AppDbContextModelSnapshot, ModelSnapshot

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "Warehouse.Api.Common"
Cohesion: 0.28
Nodes (4): Warehouse.Api.Common, Warehouse.Infrastructure, Warehouse.Api.Services, Warehouse.Application.Services

### Community 55 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 57 - "Product"
Cohesion: 0.38
Nodes (6): DateTime, Guid, ICollection, Product, ProductSize, UnitType

### Community 60 - "PutawayTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.33
Nodes (4): Guid, Stock, EntityTypeBuilder, StockConfiguration

## Knowledge Gaps
- **157 isolated node(s):** `SplitAndCloseDto`, `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser` (+152 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **39 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Warehouse.Api.Common` to `Controller Namespace Hub`, `EF Model Snapshot`, `Product & Stock Entities`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: Reserved/Available Qty`, `.AllocateOrderAsync`, `20260624095509_AddIdentityTables.Designer.cs`, `20260624095509_AddIdentityTables.Designer.cs`, `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `20260801201234_Add1.Designer.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`?**
  _High betweenness centrality (0.193) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `EF Model Snapshot`, `DI Composition Root`, `Migration: InitialCreate`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `.AllocateOrderAsync`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `20260624095509_AddIdentityTables.Designer.cs`, `AddReservedAvailableQuantity`, `AddConcurrencyAndStockConstraints`, `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `20260801201234_Add1.Designer.cs`, `OrderAllocationService.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`?**
  _High betweenness centrality (0.127) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `AppDbContext` to `PickTask API & Service`, `Products & Stocks API`, `Orders API & Allocation`, `Containers API & Mapping`, `Snapshot: InitialCreate`, `Controller Namespace Hub`, `Container`, `Locations Controller API`, `PickTask`, `PutawayTask`, `Order Service Layer`, `PickTask Entity Mapping`, `TestOrderGenerator/src/App.tsx`, `PickTaskItem`, `Product`, `PutawayTaskItem`, `Stock`?**
  _High betweenness centrality (0.103) - this node is a cross-community bridge._
- **What connects `SplitAndCloseDto`, `$schema`, `commandName` to the rest of the system?**
  _157 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.06491228070175438 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.05241090146750524 - nodes in this community are weakly interconnected._