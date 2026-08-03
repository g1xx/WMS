# Graph Report - WMS  (2026-08-03)

## Corpus Check
- 123 files · ~29,570 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 757 nodes · 1122 edges · 67 communities (31 shown, 36 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 12 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `65ca4a71`
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

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Api.DTOs` - 33 edges
2. `Warehouse.Infrastructure.Migrations` - 31 edges
3. `Warehouse.Domain` - 26 edges
4. `compilerOptions` - 17 edges
5. `AppDbContext` - 17 edges
6. `compilerOptions` - 17 edges
7. `Container` - 16 edges
8. `Warehouse.Infrastructure` - 16 edges
9. `PickTaskService` - 15 edges
10. `compilerOptions` - 15 edges

## Surprising Connections (you probably didn't know these)
- `OrderAllocationService` --implements--> `IOrderAllocationService`  [EXTRACTED]
  Backend/Warehouse.Api/Services/OrderAllocationService.cs → Backend/Warehouse.Api/Services/IOrderAllocationService.cs
- `PickTaskService` --implements--> `IPickTaskService`  [EXTRACTED]
  Backend/Warehouse.Api/Services/PickTaskService.cs → Backend/Warehouse.Api/Services/IPickTaskService.cs
- `Container` --references--> `Location`  [EXTRACTED]
  Backend/Warehouse.Domain/Container.cs → Backend/Warehouse.Domain/Location.cs
- `PickTask` --references--> `Container`  [EXTRACTED]
  Backend/Warehouse.Domain/PickTask.cs → Backend/Warehouse.Domain/Container.cs
- `AppDbContext` --references--> `Container`  [EXTRACTED]
  Backend/Warehouse.Infrastructure/AppDbContext.cs → Backend/Warehouse.Domain/Container.cs

## Import Cycles
- None detected.

## Communities (67 total, 36 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.12
Nodes (21): ActionResult, DispatchContainerDto, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, PickItemDto (+13 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.09
Nodes (21): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+13 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.07
Nodes (27): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, axiosClient, logout(), App() (+19 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.11
Nodes (21): ActionResult, AppDbContext, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController (+13 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.14
Nodes (18): ActionResult, AppDbContext, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController (+10 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.21
Nodes (5): ContainerMoveDto, StartPickTaskDto, Warehouse.Api.DTOs, Warehouse.Infrastructure, Warehouse.Api.Controllers

### Community 9 - "Auth Controller & Login"
Cohesion: 0.21
Nodes (12): ActionResult, Guid, HttpPost, IActionResult, Task, AuthController, IConfiguration, IdentityUser (+4 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.13
Nodes (19): ActionResult, AppDbContext, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List (+11 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.07
Nodes (23): AppDbContext, Guid, Task, OrderService, net10.0, Microsoft.NET.Sdk, ModelBuilder, AddOrderItemPendingReplenishment (+15 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 14 - "Order Service Layer"
Cohesion: 0.48
Nodes (3): Guid, Task, IOrderService

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.31
Nodes (3): LocationCreateDto, Warehouse.Domain, Warehouse.Infrastructure.Configurations

### Community 17 - "TestOrderGenerator/package.json"
Cohesion: 0.07
Nodes (28): axios, dependencies, axios, react, react-dom, devDependencies, @types/node, @types/react (+20 more)

### Community 18 - "compilerOptions"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 19 - "compilerOptions"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 20 - "TestOrderGenerator/src/App.tsx"
Cohesion: 0.20
Nodes (13): axiosClient, App(), extractErrorMessage(), randomInt(), CreatedOrder, LogEntry, LogStatus, OrderCreatePayload (+5 more)

### Community 21 - "Product"
Cohesion: 0.06
Nodes (31): Guid, OrderItem, DateTime, Guid, ICollection, PickTask, PickTaskStatus, Guid (+23 more)

### Community 22 - "DI Composition Root"
Cohesion: 0.25
Nodes (6): AppDbContext, Guid, IsAllocated, Message, Task, OrderAllocationService

### Community 30 - "Migration: Orders Constraints"
Cohesion: 0.40
Nodes (3): MigrationBuilder, ConfigureOrdersConstraints, Migration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.09
Nodes (20): Result, ResultErrorType, ActionResult, HttpPost, Task, InventoryController, Guid, AdjustStockDto (+12 more)

### Community 37 - "AppDbContext"
Cohesion: 0.12
Nodes (15): Guid, List, ReportDefectResultDto, ReportMissingItemDto, AppDbContext, DispatchContainerDto, Guid, IEnumerable (+7 more)

### Community 39 - "Snapshot: ProductVolumetrics"
Cohesion: 0.29
Nodes (5): Guid, PickTaskItemResponseDto, Guid, List, PickTaskResponseDto

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.36
Nodes (3): Warehouse.Api.Common, Warehouse.Api.Services, Warehouse.Application.Services

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.14
Nodes (8): ModelBuilder, AddProductVolumetrics, ModelBuilder, AddPickTasksAndItems1, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 57 - "20260801193412_AddOrderItemPendingReplenishment.Designer.cs"
Cohesion: 0.50
Nodes (4): Guid, List, OrderCreateDto, OrderItemCreateDto

## Knowledge Gaps
- **154 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)`, `Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2)` (+149 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **36 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure.Migrations` connect `.AllocateOrderAsync` to `Project Build Targets`, `EF Model Snapshot`, `Product & Stock Entities`, `Migration: PiecePackageProduct`, `Migration: InitialCreate`, `Migration: ProductUpdatedAt`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `20260703175827_AddContainersStock.Designer.cs`, `Add1`, `20260624095509_AddIdentityTables.Designer.cs`?**
  _High betweenness centrality (0.133) - this node is a cross-community bridge._
- **Why does `Warehouse.Api.DTOs` connect `Controller Namespace Hub` to `RegisterDto.cs`, `PickTask API & Service`, `Products & Stocks API`, `Snapshot: InitialCreate`, `AppDbContext`, `Snapshot: ProductVolumetrics`, `Snapshot: ProductVolumetrics1`, `PickTask Entity Mapping`, `SplitAndCloseDto.cs`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `LocationResponseDto.cs`, `DispatchContainerDto.cs`, `LoginDto.cs`, `PickItemDto.cs`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `Snapshot: PiecePackageProduct`, `Snapshot: Locations & Containers`, `Snapshot: Orders & OrderItems`, `Snapshot: Orders Constraints`, `Snapshot: PickTasks & Items`, `EF Model Snapshot`, `Snapshot: Reserved/Available Qty`, `.AllocateOrderAsync`, `Product & Stock Entities`, `20260624095509_AddIdentityTables.Designer.cs`, `Product`, `20260703175827_AddContainersStock.Designer.cs`, `20260624095509_AddIdentityTables.Designer.cs`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **What connects `net10.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9)` to the rest of the system?**
  _154 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.12195121951219512 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.09113300492610837 - nodes in this community are weakly interconnected._