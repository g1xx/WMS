# Graph Report - WMS  (2026-08-01)

## Corpus Check
- 120 files · ~28,047 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 710 nodes · 1106 edges · 58 communities (27 shown, 31 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 12 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f2135609`
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

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Api.DTOs` - 33 edges
2. `Warehouse.Infrastructure.Migrations` - 29 edges
3. `Warehouse.Domain` - 26 edges
4. `Warehouse.Infrastructure` - 26 edges
5. `AppDbContext` - 24 edges
6. `compilerOptions` - 17 edges
7. `compilerOptions` - 17 edges
8. `Container` - 16 edges
9. `PickTaskService` - 15 edges
10. `compilerOptions` - 15 edges

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

## Communities (58 total, 31 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.12
Nodes (17): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task, PickTaskController (+9 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.12
Nodes (15): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+7 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.08
Nodes (25): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, axiosClient, App(), extractErrorMessage() (+17 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.08
Nodes (28): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+20 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.10
Nodes (23): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto (+15 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.24
Nodes (5): StocksController, Warehouse.Api.Common, Warehouse.Api.DTOs, Warehouse.Api.Controllers, Warehouse.Application.Services

### Community 9 - "Auth Controller & Login"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+4 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.17
Nodes (12): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+4 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.13
Nodes (17): Warehouse.Api, net10.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), Warehouse.Domain, net10.0, Microsoft.NET.Sdk, Warehouse.Infrastructure (+9 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 13 - "EF Model Snapshot"
Cohesion: 0.20
Nodes (5): ModelBuilder, InitialCreate, ModelBuilder, AddIdentityTables, Warehouse.Infrastructure.Migrations

### Community 14 - "Order Service Layer"
Cohesion: 0.26
Nodes (6): Guid, Task, IOrderService, Guid, Task, OrderService

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
Cohesion: 0.18
Nodes (13): axiosClient, App(), extractErrorMessage(), randomInt(), CreatedOrder, LogEntry, LogStatus, OrderCreatePayload (+5 more)

### Community 21 - "Product"
Cohesion: 0.06
Nodes (34): ActionResult, HttpGet, HttpPost, IEnumerable, Task, Guid, ICollection, Location (+26 more)

### Community 26 - "Migration: ProductVolumetrics"
Cohesion: 0.50
Nodes (3): MigrationBuilder, AddProductVolumetrics, Migration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.11
Nodes (17): Result, ResultErrorType, ActionResult, HttpPost, Task, InventoryController, Guid, AdjustStockDto (+9 more)

### Community 37 - "AppDbContext"
Cohesion: 0.14
Nodes (13): Guid, PickTaskItemResponseDto, Guid, List, PickTaskResponseDto, Guid, List, ReportDefectResultDto (+5 more)

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.40
Nodes (3): ModelBuilder, AppDbContextModelSnapshot, ModelSnapshot

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

## Knowledge Gaps
- **146 isolated node(s):** `SplitAndCloseDto`, `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser` (+141 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **31 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Snapshot: ProductVolumetrics1` to `Containers API & Mapping`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Controller Namespace Hub`, `Snapshot: Locations & Containers`, `Locations Controller API`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `EF Model Snapshot`, `Snapshot: PickTasks & Items`, `Snapshot: Reserved/Available Qty`, `Product & Stock Entities`, `.AllocateOrderAsync`, `20260624095509_AddIdentityTables.Designer.cs`, `Product`, `DI Composition Root`, `20260703175827_AddContainersStock.Designer.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`?**
  _High betweenness centrality (0.175) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `EF Model Snapshot` to `Product & Stock Entities`, `DI Composition Root`, `Migration: PiecePackageProduct`, `Migration: InitialCreate`, `Migration: ProductUpdatedAt`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `.AllocateOrderAsync`, `20260624095509_AddIdentityTables.Designer.cs`, `20260703175827_AddContainersStock.Designer.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Product` to `Products & Stocks API`, `Orders API & Allocation`, `Containers API & Mapping`, `Snapshot: InitialCreate`, `AppDbContext`, `Controller Namespace Hub`, `Locations Controller API`, `Order Service Layer`?**
  _High betweenness centrality (0.099) - this node is a cross-community bridge._
- **What connects `SplitAndCloseDto`, `$schema`, `commandName` to the rest of the system?**
  _146 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.12439024390243902 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.12380952380952381 - nodes in this community are weakly interconnected._