# Graph Report - WMS  (2026-07-31)

## Corpus Check
- 97 files · ~21,004 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 554 nodes · 849 edges · 49 communities (20 shown, 29 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 8 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `74d46e59`
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
- Order Entity Mapping
- Location Entity Mapping
- OrderItem Entity Mapping
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
- Snapshot: PiecePackageProduct
- Snapshot: ProductVolumetrics
- Snapshot: ProductVolumetrics1
- Snapshot: Locations & Containers
- Snapshot: Orders & OrderItems
- Snapshot: Orders Constraints
- Snapshot: PickTasks & Items
- Snapshot: Containers Stock
- Snapshot: Reserved/Available Qty
- EF Configuration Namespace
- TS Project References
- React + TypeScript + Vite
- CLAUDE.md

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Infrastructure.Migrations` - 27 edges
2. `Warehouse.Api.DTOs` - 24 edges
3. `Warehouse.Domain` - 24 edges
4. `Warehouse.Infrastructure` - 22 edges
5. `AppDbContext` - 22 edges
6. `compilerOptions` - 17 edges
7. `Container` - 15 edges
8. `compilerOptions` - 15 edges
9. `Order` - 13 edges
10. `PickTaskService` - 11 edges

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

## Communities (49 total, 29 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.10
Nodes (21): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task, PickTaskController (+13 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): axios, dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.09
Nodes (21): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+13 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.11
Nodes (16): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, axiosClient, App(), Login() (+8 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.09
Nodes (24): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+16 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.14
Nodes (15): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, ContainerMoveDto (+7 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.23
Nodes (6): SplitAndCloseDto, Warehouse.Api.DTOs, Warehouse.Infrastructure, Warehouse.Api.Controllers, Warehouse.Domain, Warehouse.Api.Services

### Community 9 - "Auth Controller & Login"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, LoginDto (+4 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.18
Nodes (13): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+5 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.12
Nodes (18): Warehouse.Api, net10.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), Warehouse.Domain, net10.0, Microsoft.NET.Sdk, Warehouse.Infrastructure (+10 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 13 - "EF Model Snapshot"
Cohesion: 0.20
Nodes (6): ModelBuilder, AddProductUpdatedAt, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

### Community 14 - "Order Service Layer"
Cohesion: 0.26
Nodes (6): Guid, Task, IOrderService, Guid, Task, OrderService

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.05
Nodes (37): Guid, ICollection, Location, Guid, OrderItem, DateTime, Guid, ICollection (+29 more)

### Community 16 - "Product & Stock Entities"
Cohesion: 0.17
Nodes (12): AppDbContext, PickTaskService, DispatchContainerDto, Guid, IEnumerable, IPickTaskService, PickItemDto, PickTask (+4 more)

### Community 24 - "Migration: InitialCreate"
Cohesion: 0.40
Nodes (3): MigrationBuilder, InitialCreate, Migration

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

## Knowledge Gaps
- **87 isolated node(s):** `SplitAndCloseDto`, `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser` (+82 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **29 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `Snapshot: InitialCreate`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: ProductVolumetrics1`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `EF Model Snapshot`, `Snapshot: Containers Stock`, `EF Configuration Namespace`, `Snapshot: Reserved/Available Qty`, `DI Composition Root`?**
  _High betweenness centrality (0.247) - this node is a cross-community bridge._
- **Why does `Warehouse.Api.DTOs` connect `Controller Namespace Hub` to `PickTask API & Service`, `Products & Stocks API`, `Orders API & Allocation`, `Containers API & Mapping`, `Auth Controller & Login`?**
  _High betweenness centrality (0.163) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `EF Model Snapshot` to `DI Composition Root`, `Migration: PiecePackageProduct`, `Migration: InitialCreate`, `Migration: ProductUpdatedAt`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: InitialCreate`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `Snapshot: ProductVolumetrics1`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `Snapshot: Containers Stock`, `Snapshot: Reserved/Available Qty`, `EF Configuration Namespace`?**
  _High betweenness centrality (0.160) - this node is a cross-community bridge._
- **What connects `SplitAndCloseDto`, `$schema`, `commandName` to the rest of the system?**
  _87 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.10077519379844961 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.09113300492610837 - nodes in this community are weakly interconnected._