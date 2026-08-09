# Graph Report - WMS  (2026-08-09)

## Corpus Check
- 185 files · ~44,868 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1166 nodes · 2106 edges · 81 communities (45 shown, 36 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 66 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1e53224b`
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
- LocationsController
- API Launch Settings
- EF Model Snapshot
- IEntityTypeConfiguration
- PickTask Entity Mapping
- Product & Stock Entities
- TestOrderGenerator/package.json
- compilerOptions
- compilerOptions
- TestOrderGenerator/src/App.tsx
- Product
- DI Composition Root
- Migration: PiecePackageProduct
- PutawayTaskConfiguration
- Migration: ProductUpdatedAt
- Migration: ProductVolumetrics
- Migration: ProductVolumetrics1
- Migration: Locations & Containers
- Migration: Orders & OrderItems
- Migration: Orders Constraints
- Migration: PickTasks & Items
- Migration: PickTasks & Items 1
- Migration: Identity Tables
- Fact
- Migration: Reserved/Available Qty
- Snapshot: InitialCreate
- Mock
- Snapshot: PiecePackageProduct
- XminConcurrencyExtensions.cs
- Task
- StockTransaction
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
- .AllocateOrderAsync
- Add1
- PutawayTaskItem
- Stock
- AddReservedAvailableQuantity
- AddConcurrencyAndStockConstraints
- RegisterDto.cs
- 20260618193047_AddProductVolumetrics.Designer.cs
- DateTime
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- OrderAllocationService.cs
- Stock
- 20260804092556_AddPutawayTables.Designer.cs
- AddStockTransactionJournal
- 20260615013749_AddPiecePackageProduct.Designer.cs
- PickTaskItemResponseDto
- 20260618193047_AddProductVolumetrics.Designer.cs
- AdjustStockDto.cs
- react
- ContainerMoveDto.cs
- Product
- LoginDto.cs
- InitialCreate
- 20260801201234_Add1.Designer.cs

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Domain` - 52 edges
2. `Warehouse.Application.DTOs` - 47 edges
3. `Warehouse.Infrastructure.Migrations` - 39 edges
4. `Container` - 33 edges
5. `Result` - 31 edges
6. `Warehouse.Application.Interfaces` - 31 edges
7. `PickTask` - 29 edges
8. `PutawayTask` - 26 edges
9. `Location` - 25 edges
10. `IUnitOfWork` - 24 edges

## Surprising Connections (you probably didn't know these)
- `ContainersController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ContainersController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `LocationsController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/LocationsController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `OrdersController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/OrdersController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `ProductsController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ProductsController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `StocksController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/StocksController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs

## Import Cycles
- None detected.

## Communities (81 total, 36 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.07
Nodes (31): Guid, PickTaskItemResponseDto, Guid, List, PickTaskResponseDto, Guid, List, ReportDefectResultDto (+23 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.09
Nodes (23): ActionResult, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, IEnumerable (+15 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.13
Nodes (23): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+15 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.05
Nodes (36): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+28 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.09
Nodes (22): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto (+14 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.15
Nodes (14): ActionResult, Guid, HttpPost, IActionResult, IConfiguration, IdentityUser, Task, AuthController (+6 more)

### Community 9 - "Auth Controller & Login"
Cohesion: 0.09
Nodes (27): Warehouse.Api, net10.0, Warehouse.Application.Tests, net10.0, Microsoft.NET.Sdk, Warehouse.Application, net10.0, Microsoft.NET.Sdk (+19 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.06
Nodes (33): ActionResult, HttpPost, Task, InventoryController, ActionResult, DateTime, Guid, HttpGet (+25 more)

### Community 11 - "LocationsController"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+4 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 14 - "IEntityTypeConfiguration"
Cohesion: 0.22
Nodes (5): EntityTypeBuilder, ContainerConfiguration, EntityTypeBuilder, PickTaskConfiguration, IEntityTypeConfiguration

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.14
Nodes (14): Guid, List, Task, IPickTaskRepository, DateTime, Guid, ICollection, PickTask (+6 more)

### Community 16 - "Product & Stock Entities"
Cohesion: 0.14
Nodes (8): ModelBuilder, AddProductUpdatedAt, ModelBuilder, AddContainersStock, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

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
Cohesion: 0.07
Nodes (31): AllowAnonymous, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, Task (+23 more)

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.18
Nodes (7): axios, AxiosRequestConfig, logout(), Props, Props, PendingFlow, Screen

### Community 28 - "Migration: Locations & Containers"
Cohesion: 0.22
Nodes (5): MigrationBuilder, RefactorLocationsAndAddContainers, MigrationBuilder, AddContainersStock, Migration

### Community 39 - "XminConcurrencyExtensions.cs"
Cohesion: 0.22
Nodes (5): EntityTypeBuilder, StockConfiguration, EntityTypeBuilder, XminConcurrencyExtensions, Warehouse.Infrastructure.Configurations

### Community 41 - "StockTransaction"
Cohesion: 0.18
Nodes (9): IStockTransactionRepository, DateTime, Guid, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration, AppDbContext (+1 more)

### Community 42 - "PickTask"
Cohesion: 0.24
Nodes (8): fetchSupervisorAuthHeader(), isSupervisorAuthError(), Props, Props, PickTasks(), Props, PickTask, PickTaskItem

### Community 43 - "PutawayTask"
Cohesion: 0.14
Nodes (15): Guid, List, Task, IPutawayTaskRepository, DateTime, Guid, ICollection, PutawayTask (+7 more)

### Community 44 - "AppDbContext"
Cohesion: 0.22
Nodes (7): Guid, IdentityUser, ModelBuilder, AppDbContext, DbSet, IdentityDbContext, IdentityRole

### Community 46 - "Snapshot: Reserved/Available Qty"
Cohesion: 0.08
Nodes (13): ModelBuilder, InitialCreate, ModelBuilder, AddProductVolumetrics, ModelBuilder, AddPickTasksAndItems1, ModelBuilder, AddIdentityTables (+5 more)

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.29
Nodes (5): Props, Phase, Props, PutawayTask, PutawayTaskItem

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "Warehouse.Api.Common"
Cohesion: 0.08
Nodes (16): ResultExtensions, ConcurrencyConflictException, SplitAndCloseDto, IConfiguration, DependencyInjection, Warehouse.Api.Common, Warehouse.Application.Common, Warehouse.Infrastructure.Repositories (+8 more)

### Community 55 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 57 - ".AllocateOrderAsync"
Cohesion: 0.25
Nodes (10): OrderAllocationServiceTests, Fact, Guid, Location, Mock, Order, OrderAllocationService, OrderItem (+2 more)

### Community 60 - "PutawayTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.19
Nodes (11): Guid, List, Task, IStockRepository, Guid, Stock, AppDbContext, Guid (+3 more)

### Community 64 - "RegisterDto.cs"
Cohesion: 0.10
Nodes (19): Dictionary, Guid, IEnumerable, List, Task, ILocationRepository, Guid, ICollection (+11 more)

### Community 70 - "Stock"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 76 - "PickTaskItemResponseDto"
Cohesion: 0.32
Nodes (7): extractErrorMessage(), getAvailableQuantity(), inputStyle, InventoryAdmin(), labelStyle, Product, ProductStock

### Community 77 - "20260618193047_AddProductVolumetrics.Designer.cs"
Cohesion: 0.48
Nodes (3): Guid, Task, IOrderService

### Community 78 - "AdjustStockDto.cs"
Cohesion: 0.38
Nodes (3): axiosClient, App(), Login()

### Community 79 - "react"
Cohesion: 0.29
Nodes (3): Mode, Props, react

### Community 81 - "Product"
Cohesion: 0.11
Nodes (19): DateTime, Dictionary, Guid, List, Task, IProductRepository, DateTime, Guid (+11 more)

## Knowledge Gaps
- **165 isolated node(s):** `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser`, `applicationUrl` (+160 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **36 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Snapshot: Reserved/Available Qty` to `20260618193047_AddProductVolumetrics.Designer.cs`, `Migration: Reserved/Available Qty`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `Snapshot: PiecePackageProduct`, `20260804092556_AddPutawayTables.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `AppDbContext`, `EF Model Snapshot`, `Product & Stock Entities`, `ContainerMoveDto.cs`, `LoginDto.cs`, `20260624095509_AddIdentityTables.Designer.cs`, `Warehouse.Api.Common`, `20260801201234_Add1.Designer.cs`?**
  _High betweenness centrality (0.199) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `EF Model Snapshot`, `Product`, `DI Composition Root`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `AddReservedAvailableQuantity`, `AddConcurrencyAndStockConstraints`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `OrderAllocationService.cs`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `ContainerMoveDto.cs`, `LoginDto.cs`, `InitialCreate`, `20260801201234_Add1.Designer.cs`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `AppDbContext` to `RegisterDto.cs`, `Orders API & Allocation`, `Containers API & Mapping`, `Stock`, `StockTransaction`, `PutawayTask`, `PickTask Entity Mapping`, `Product`, `PickTaskItem`, `PutawayTaskItem`, `Stock`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **What connects `$schema`, `commandName`, `dotnetRunMessages` to the rest of the system?**
  _165 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.06826923076923076 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.09411764705882353 - nodes in this community are weakly interconnected._