# Graph Report - WMS  (2026-08-09)

## Corpus Check
- 179 files · ~42,128 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1070 nodes · 1869 edges · 84 communities (43 shown, 41 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 46 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d63afafe`
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
- 20260618193047_AddProductVolumetrics.Designer.cs
- AdjustStockDto.cs
- StockAdjustmentResultDto.cs
- ContainerMoveDto.cs
- CreateProductWithLocationDto.cs
- LoginDto.cs
- RegisterDto.cs

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Domain` - 50 edges
2. `Warehouse.Application.DTOs` - 45 edges
3. `Warehouse.Infrastructure.Migrations` - 39 edges
4. `Result` - 31 edges
5. `Warehouse.Infrastructure` - 27 edges
6. `PickTask` - 26 edges
7. `Container` - 25 edges
8. `PutawayTask` - 25 edges
9. `Warehouse.Application.Interfaces` - 24 edges
10. `AppDbContext` - 23 edges

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

## Communities (84 total, 41 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.08
Nodes (29): ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task (+21 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.06
Nodes (33): ActionResult, HttpPost, Task, InventoryController, ActionResult, DateTime, Guid, HttpGet (+25 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.13
Nodes (23): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+15 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.07
Nodes (30): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+22 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.10
Nodes (20): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto (+12 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.27
Nodes (5): SplitAndCloseDto, Warehouse.Api.Common, Warehouse.Infrastructure, Warehouse.Api.Controllers, Warehouse.Application.DTOs

### Community 9 - "Auth Controller & Login"
Cohesion: 0.15
Nodes (14): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+6 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+4 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.12
Nodes (20): Warehouse.Api, net10.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), Warehouse.Application, net10.0, Microsoft.NET.Sdk, Warehouse.Domain (+12 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 14 - "Order Service Layer"
Cohesion: 0.08
Nodes (25): Guid, DispatchContainerResultDto, Guid, List, PutawayTaskItemResponseDto, PutawayTaskResponseDto, Guid, List (+17 more)

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.14
Nodes (14): Guid, List, Task, IPickTaskRepository, DateTime, Guid, ICollection, PickTask (+6 more)

### Community 16 - "Product & Stock Entities"
Cohesion: 0.20
Nodes (6): ModelBuilder, AddProductVolumetrics, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

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
Cohesion: 0.10
Nodes (20): AllowAnonymous, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, Task (+12 more)

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.18
Nodes (7): axios, AxiosRequestConfig, logout(), Props, Props, PendingFlow, Screen

### Community 30 - "Migration: Orders Constraints"
Cohesion: 0.40
Nodes (3): MigrationBuilder, AddPiecePackageProduct, Migration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.11
Nodes (17): Dictionary, Guid, List, Task, IProductRepository, DateTime, Guid, ICollection (+9 more)

### Community 37 - "AppDbContext"
Cohesion: 0.12
Nodes (14): Dictionary, List, Task, ILocationRepository, Guid, ICollection, Location, LocationType (+6 more)

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.12
Nodes (11): EntityTypeBuilder, ContainerConfiguration, LocationConfiguration, EntityTypeBuilder, PickTaskConfiguration, EntityTypeBuilder, PutawayTaskConfiguration, EntityTypeBuilder (+3 more)

### Community 41 - "Container"
Cohesion: 0.16
Nodes (9): IStockTransactionRepository, DateTime, Guid, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration, AppDbContext (+1 more)

### Community 42 - "PickTask"
Cohesion: 0.24
Nodes (8): fetchSupervisorAuthHeader(), isSupervisorAuthError(), Props, Props, PickTasks(), Props, PickTask, PickTaskItem

### Community 43 - "PutawayTask"
Cohesion: 0.14
Nodes (15): Guid, List, Task, IPutawayTaskRepository, DateTime, Guid, ICollection, PutawayTask (+7 more)

### Community 44 - "AppDbContext"
Cohesion: 0.13
Nodes (11): Guid, OrderItem, Guid, IdentityUser, ModelBuilder, AppDbContext, EntityTypeBuilder, OrderItemConfiguration (+3 more)

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.29
Nodes (5): Props, Phase, Props, PutawayTask, PutawayTaskItem

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "Warehouse.Api.Common"
Cohesion: 0.16
Nodes (5): Warehouse.Application.Common, Warehouse.Infrastructure.Repositories, Warehouse.Domain, Warehouse.Application.Services, Warehouse.Application.Interfaces

### Community 55 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 57 - "Product"
Cohesion: 0.36
Nodes (4): AppDbContext, Task, UnitOfWork, IDbContextTransaction

### Community 60 - "PutawayTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.15
Nodes (13): Guid, List, Task, IStockRepository, Guid, Stock, EntityTypeBuilder, StockConfiguration (+5 more)

### Community 68 - "20260801201234_Add1.Designer.cs"
Cohesion: 0.29
Nodes (3): Mode, Props, react

### Community 76 - "PickTaskItemResponseDto"
Cohesion: 0.32
Nodes (7): extractErrorMessage(), getAvailableQuantity(), inputStyle, InventoryAdmin(), labelStyle, Product, ProductStock

### Community 77 - "20260618193047_AddProductVolumetrics.Designer.cs"
Cohesion: 0.48
Nodes (3): Guid, Task, IOrderService

### Community 78 - "AdjustStockDto.cs"
Cohesion: 0.38
Nodes (3): axiosClient, App(), Login()

## Knowledge Gaps
- **159 isolated node(s):** `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser`, `applicationUrl` (+154 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `EF Model Snapshot`, `Product & Stock Entities`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `AppDbContext`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Warehouse.Api.Common`, `20260624095509_AddIdentityTables.Designer.cs`, `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `ContainerMoveDto.cs`, `CreateProductWithLocationDto.cs`, `LoginDto.cs`?**
  _High betweenness centrality (0.198) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `EF Model Snapshot`, `Product`, `DI Composition Root`, `Migration: InitialCreate`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `20260624095509_AddIdentityTables.Designer.cs`, `AddReservedAvailableQuantity`, `AddConcurrencyAndStockConstraints`, `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `OrderAllocationService.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `ContainerMoveDto.cs`, `CreateProductWithLocationDto.cs`, `LoginDto.cs`?**
  _High betweenness centrality (0.134) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `AppDbContext` to `Products & Stocks API`, `Orders API & Allocation`, `AppDbContext`, `Containers API & Mapping`, `Snapshot: InitialCreate`, `Container`, `Locations Controller API`, `PutawayTask`, `PickTask Entity Mapping`, `PickTaskItem`, `PutawayTaskItem`, `Stock`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **What connects `$schema`, `commandName`, `dotnetRunMessages` to the rest of the system?**
  _159 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.075990675990676 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.05612244897959184 - nodes in this community are weakly interconnected._