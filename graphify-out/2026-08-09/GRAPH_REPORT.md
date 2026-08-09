# Graph Report - WMS  (2026-08-09)

## Corpus Check
- 160 files · ~41,188 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 946 nodes · 1482 edges · 88 communities (41 shown, 47 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 18 edges (avg confidence: 0.8)
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
- SplitAndCloseDto.cs
- StartPickTaskDto.cs
- IsAllocated
- Message

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Api.DTOs` - 45 edges
2. `Warehouse.Infrastructure.Migrations` - 39 edges
3. `Warehouse.Domain` - 34 edges
4. `Warehouse.Infrastructure` - 32 edges
5. `Result` - 31 edges
6. `AppDbContext` - 27 edges
7. `compilerOptions` - 17 edges
8. `compilerOptions` - 17 edges
9. `Container` - 16 edges
10. `PutawayTaskResponseDto` - 15 edges

## Surprising Connections (you probably didn't know these)
- `ContainersController` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ContainersController.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `PickTaskService` --implements--> `IPickTaskService`  [EXTRACTED]
  Backend/Warehouse.Api/Services/PickTaskService.cs → Backend/Warehouse.Api/Services/IPickTaskService.cs
- `InventoryService` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Services/InventoryService.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `OrderAllocationService` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Services/OrderAllocationService.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs
- `PickTaskService` --references--> `AppDbContext`  [EXTRACTED]
  Backend/Warehouse.Api/Services/PickTaskService.cs → Backend/Warehouse.Infrastructure/AppDbContext.cs

## Import Cycles
- None detected.

## Communities (88 total, 47 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.10
Nodes (26): ActionResult, Authorize, DispatchContainerDto, Guid, HttpGet, HttpPost, IActionResult, IEnumerable (+18 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.07
Nodes (29): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, Task, ProductsController (+21 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.12
Nodes (23): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+15 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.11
Nodes (21): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+13 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.14
Nodes (19): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, ContainersController, Guid (+11 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.26
Nodes (3): Warehouse.Infrastructure, Warehouse.Api.Controllers, Warehouse.Domain

### Community 9 - "Auth Controller & Login"
Cohesion: 0.18
Nodes (14): ActionResult, Guid, HttpPost, IActionResult, IdentityUser, Task, AuthController, SupervisorOverrideDto (+6 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.13
Nodes (18): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+10 more)

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
Cohesion: 0.14
Nodes (14): Guid, DispatchContainerResultDto, DispatchContainerDto, Guid, IEnumerable, PickItemDto, PickTask, PickTaskResponseDto (+6 more)

### Community 16 - "Product & Stock Entities"
Cohesion: 0.11
Nodes (10): ModelBuilder, AddPickTasksAndItems, ModelBuilder, Add1, ModelBuilder, AddStockTransactionJournal, ModelBuilder, AppDbContextModelSnapshot (+2 more)

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
Nodes (31): AllowAnonymous, Result, ResultErrorType, ActionResult, ResultExtensions, ActionResult, Authorize, Guid (+23 more)

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.05
Nodes (32): Mode, axios, axiosClient, AxiosRequestConfig, fetchSupervisorAuthHeader(), isSupervisorAuthError(), logout(), App() (+24 more)

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 37 - "AppDbContext"
Cohesion: 0.11
Nodes (18): AdjustStockDto, ActionResult, CreateProductWithLocationDto, HttpPost, Task, InventoryController, CreateProductWithLocationDto, Guid (+10 more)

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.33
Nodes (3): EntityTypeBuilder, XminConcurrencyExtensions, Warehouse.Infrastructure.Configurations

### Community 41 - "Container"
Cohesion: 0.24
Nodes (7): DateTime, Guid, Product, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration

### Community 42 - "PickTask"
Cohesion: 0.50
Nodes (3): EntityTypeBuilder, PickTask, PickTaskConfiguration

### Community 43 - "PutawayTask"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, PutawayTask, PutawayTaskStatus, EntityTypeBuilder, PutawayTaskConfiguration

### Community 44 - "AppDbContext"
Cohesion: 0.15
Nodes (12): Guid, IdentityUser, ModelBuilder, Order, PickTask, Product, Stock, AppDbContext (+4 more)

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.29
Nodes (6): Guid, Task, OrderAllocationService, IOrderAllocationService, IsAllocated, Message

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "Warehouse.Api.Common"
Cohesion: 0.18
Nodes (7): DispatchContainerDto, PickItemDto, ReportDefectDto, Warehouse.Api.Common, Warehouse.Api.DTOs, Warehouse.Api.Services, Warehouse.Application.Services

### Community 55 - "PickTaskItem"
Cohesion: 0.25
Nodes (6): Guid, PickTask, Product, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 57 - "Product"
Cohesion: 0.50
Nodes (3): EntityTypeBuilder, Order, OrderConfiguration

### Community 60 - "PutawayTaskItem"
Cohesion: 0.29
Nodes (5): Guid, Product, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.25
Nodes (6): EntityTypeBuilder, ContainerConfiguration, EntityTypeBuilder, Stock, StockConfiguration, IEntityTypeConfiguration

### Community 63 - "AddConcurrencyAndStockConstraints"
Cohesion: 0.40
Nodes (3): MigrationBuilder, AddConcurrencyAndStockConstraints, Migration

### Community 68 - "20260801201234_Add1.Designer.cs"
Cohesion: 0.50
Nodes (3): Guid, List, ReportDefectResultDto

### Community 76 - "PickTaskItemResponseDto"
Cohesion: 0.29
Nodes (5): Guid, PickTaskItemResponseDto, Guid, List, PickTaskResponseDto

## Knowledge Gaps
- **167 isolated node(s):** `Mode`, `CreatePutawayItemPayload`, `axios`, `AxiosRequestConfig`, `Props` (+162 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **47 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Controller Namespace Hub` to `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `Migration: Reserved/Available Qty`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `20260804092556_AddPutawayTables.Designer.cs`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `Stock`, `20260618193047_AddProductVolumetrics.Designer.cs`, `EF Model Snapshot`, `Snapshot: Reserved/Available Qty`, `Product & Stock Entities`, `20260624095509_AddIdentityTables.Designer.cs`, `Warehouse.Api.Common`, `20260624095509_AddIdentityTables.Designer.cs`?**
  _High betweenness centrality (0.193) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `EF Model Snapshot`, `Product`, `DI Composition Root`, `Migration: InitialCreate`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Containers Stock`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `Snapshot: ProductVolumetrics`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `20260624095509_AddIdentityTables.Designer.cs`, `AddReservedAvailableQuantity`, `AddConcurrencyAndStockConstraints`, `RegisterDto.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `OrderAllocationService.cs`, `Stock`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260611180526_InitialCreate.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **Why does `Warehouse.Api.DTOs` connect `Warehouse.Api.Common` to `PickTask API & Service`, `Products & Stocks API`, `Orders API & Allocation`, `20260801201234_Add1.Designer.cs`, `Controller Namespace Hub`, `Auth Controller & Login`, `PickTaskItemResponseDto`, `AdjustStockDto.cs`, `PickTask Entity Mapping`, `ContainerMoveDto.cs`, `CreateProductWithLocationDto.cs`, `LoginDto.cs`, `RegisterDto.cs`, `TestOrderGenerator/src/App.tsx`, `SplitAndCloseDto.cs`, `StartPickTaskDto.cs`, `StockAdjustmentResultDto.cs`?**
  _High betweenness centrality (0.108) - this node is a cross-community bridge._
- **What connects `Mode`, `CreatePutawayItemPayload`, `axios` to the rest of the system?**
  _167 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.09990749306197964 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.06747638326585695 - nodes in this community are weakly interconnected._