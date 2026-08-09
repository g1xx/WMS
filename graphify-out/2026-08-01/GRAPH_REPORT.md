# Graph Report - WMS  (2026-08-01)

## Corpus Check
- 108 files · ~23,068 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 648 nodes · 974 edges · 57 communities (30 shown, 27 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 8 edges (avg confidence: 0.8)
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

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Infrastructure.Migrations` - 27 edges
2. `Warehouse.Api.DTOs` - 25 edges
3. `Warehouse.Domain` - 25 edges
4. `Warehouse.Infrastructure` - 24 edges
5. `AppDbContext` - 23 edges
6. `compilerOptions` - 17 edges
7. `compilerOptions` - 17 edges
8. `Container` - 16 edges
9. `compilerOptions` - 15 edges
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

## Communities (57 total, 27 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.07
Nodes (27): Result, ResultErrorType, ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable (+19 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.09
Nodes (21): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+13 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.11
Nodes (16): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, axiosClient, App(), Login() (+8 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.09
Nodes (23): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+15 more)

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
Nodes (7): Warehouse.Api.Common, Warehouse.Api.DTOs, Warehouse.Infrastructure, Warehouse.Api.Controllers, Warehouse.Domain, Warehouse.Api.Services, Warehouse.Application.Services

### Community 9 - "Auth Controller & Login"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+4 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.23
Nodes (11): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+3 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.13
Nodes (17): Warehouse.Api, net10.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), Warehouse.Domain, net10.0, Microsoft.NET.Sdk, Warehouse.Infrastructure (+9 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 13 - "EF Model Snapshot"
Cohesion: 0.14
Nodes (8): ModelBuilder, InitialCreate, ModelBuilder, AddProductVolumetrics1, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

### Community 14 - "Order Service Layer"
Cohesion: 0.26
Nodes (6): Guid, Task, IOrderService, Guid, Task, OrderService

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.18
Nodes (8): DateTime, Guid, ICollection, PickTask, PickTaskStatus, EntityTypeBuilder, PickTaskConfiguration, Warehouse.Infrastructure.Configurations

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
Cohesion: 0.24
Nodes (8): DateTime, Guid, ICollection, Product, ProductSize, UnitType, Guid, Stock

### Community 24 - "Migration: InitialCreate"
Cohesion: 0.40
Nodes (3): MigrationBuilder, InitialCreate, Migration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.28
Nodes (7): LocationCreateDto, Guid, ICollection, Location, LocationType, EntityTypeBuilder, LocationConfiguration

### Community 37 - "AppDbContext"
Cohesion: 0.25
Nodes (7): Guid, IdentityUser, ModelBuilder, AppDbContext, DbSet, IdentityDbContext, IdentityRole

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.33
Nodes (5): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration, IEntityTypeConfiguration

### Community 45 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.33
Nodes (5): Guid, IsAllocated, Message, Task, OrderAllocationService

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

## Knowledge Gaps
- **140 isolated node(s):** `SplitAndCloseDto`, `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser` (+135 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `EF Model Snapshot`, `Snapshot: Reserved/Available Qty`, `Product & Stock Entities`, `20260624095509_AddIdentityTables.Designer.cs`, `20260703175827_AddContainersStock.Designer.cs`, `DI Composition Root`?**
  _High betweenness centrality (0.172) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `EF Model Snapshot` to `Product & Stock Entities`, `DI Composition Root`, `Migration: PiecePackageProduct`, `Migration: InitialCreate`, `Migration: ProductUpdatedAt`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `20260703175827_AddContainersStock.Designer.cs`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `AppDbContext` to `PickTask API & Service`, `Products & Stocks API`, `Orders API & Allocation`, `Containers API & Mapping`, `Snapshot: InitialCreate`, `Controller Namespace Hub`, `Snapshot: ProductVolumetrics1`, `Locations Controller API`, `PickTaskItem`, `Order Service Layer`, `.AllocateOrderAsync`, `PickTask Entity Mapping`, `Product`?**
  _High betweenness centrality (0.100) - this node is a cross-community bridge._
- **What connects `SplitAndCloseDto`, `$schema`, `commandName` to the rest of the system?**
  _140 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.07481005260081823 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.09113300492610837 - nodes in this community are weakly interconnected._