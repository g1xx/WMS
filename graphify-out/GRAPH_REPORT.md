# Graph Report - WMS  (2026-08-10)

## Corpus Check
- 196 files · ~51,252 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1230 nodes · 2249 edges · 89 communities (49 shown, 40 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 72 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5d24527a`
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
- IEntityTypeConfiguration
- Migration: Reserved/Available Qty
- AddContainersStock
- Add2
- Snapshot: PiecePackageProduct
- PickTaskConfiguration
- PutawayTaskConfiguration
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
- 20260614131504_AddProductUpdatedAt.Designer.cs
- PutawayTaskItem
- Stock
- AddReservedAvailableQuantity
- AddConcurrencyAndStockConstraints
- RegisterDto.cs
- 20260618193047_AddProductVolumetrics.Designer.cs
- DateTime
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- 20260618193047_AddProductVolumetrics.Designer.cs
- OrderAllocationService.cs
- Stock
- 20260804092556_AddPutawayTables.Designer.cs
- AddStockTransactionJournal
- 20260624095509_AddIdentityTables.Designer.cs
- 20260615013749_AddPiecePackageProduct.Designer.cs
- 20260801201234_Add1.Designer.cs
- PickTaskItemResponseDto
- 20260618193047_AddProductVolumetrics.Designer.cs
- AdjustStockDto.cs
- 20260809183404_Add2.Designer.cs
- 20260809211304_RemoveLocationFromPutawayItem.Designer.cs
- Product
- AddContainerBarcodeUniqueIndex
- 20260618193047_AddProductVolumetrics.Designer.cs
- InitialCreate
- 20260624095509_AddIdentityTables.Designer.cs
- 20260805012255_AddStockTransactionJournal.Designer.cs
- 20260809235604_AddContainerBarcodeUniqueIndex.Designer.cs
- 20260809235825_Add3.Designer.cs

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Domain` - 53 edges
2. `Warehouse.Application.DTOs` - 47 edges
3. `Warehouse.Infrastructure.Migrations` - 47 edges
4. `Container` - 35 edges
5. `Warehouse.Application.Interfaces` - 32 edges
6. `Result` - 31 edges
7. `PickTask` - 29 edges
8. `Warehouse.Infrastructure` - 27 edges
9. `Location` - 26 edges
10. `PutawayTask` - 26 edges

## Surprising Connections (you probably didn't know these)
- `ContainersController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ContainersController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `LocationsController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/LocationsController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `OrdersController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/OrdersController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs
- `PickTaskController` --references--> `IPickTaskService`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/PickTaskController.cs → Backend/Warehouse.Application/Services/IPickTaskService.cs
- `ProductsController` --references--> `IUnitOfWork`  [EXTRACTED]
  Backend/Warehouse.Api/Controllers/ProductsController.cs → Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs

## Import Cycles
- None detected.

## Communities (89 total, 40 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.13
Nodes (12): DispatchContainerDto, Guid, DispatchContainerResultDto, ReportDefectDto, Guid, List, ReportDefectResultDto, Task (+4 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.05
Nodes (37): dependencies, axios, react, react-dom, react-router-dom, @tanstack/react-query, devDependencies, oxlint (+29 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.33
Nodes (12): fetchSupervisorAuthHeader(), ActionResultMessage, cancelPickTask(), dispatchContainer(), fetchCurrentPickTask(), pickItem(), reportDefect(), reportMissingItem() (+4 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.13
Nodes (21): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+13 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.05
Nodes (36): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, Task, OrdersController, Guid (+28 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.07
Nodes (27): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task, ContainersController (+19 more)

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
Cohesion: 0.32
Nodes (9): ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, Task (+1 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 13 - "EF Model Snapshot"
Cohesion: 0.20
Nodes (8): ActionResult, Result, ResultErrorType, MessageResponseDto, Guid, IEnumerable, Task, IPickTaskService

### Community 14 - "IEntityTypeConfiguration"
Cohesion: 0.29
Nodes (3): Mode, Props, react

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.14
Nodes (14): Guid, List, Task, IPickTaskRepository, DateTime, Guid, ICollection, PickTask (+6 more)

### Community 16 - "Product & Stock Entities"
Cohesion: 0.14
Nodes (8): ModelBuilder, AddOrderItemPendingReplenishment, ModelBuilder, RemoveLocationFromPutawayItem, ModelBuilder, AppDbContextModelSnapshot, Warehouse.Infrastructure.Migrations, ModelSnapshot

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

### Community 24 - "PutawayTaskConfiguration"
Cohesion: 0.22
Nodes (5): EntityTypeBuilder, ContainerConfiguration, EntityTypeBuilder, XminConcurrencyExtensions, Warehouse.Infrastructure.Configurations

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.18
Nodes (7): axios, AxiosRequestConfig, logout(), Props, Props, PendingFlow, Screen

### Community 29 - "Migration: Orders & OrderItems"
Cohesion: 0.15
Nodes (7): MigrationBuilder, AddOrdersAndOrderItems, MigrationBuilder, AddReservedAvailableQuantity, MigrationBuilder, Add3, Migration

### Community 34 - "IEntityTypeConfiguration"
Cohesion: 0.22
Nodes (5): EntityTypeBuilder, LocationConfiguration, EntityTypeBuilder, StockConfiguration, IEntityTypeConfiguration

### Community 35 - "Migration: Reserved/Available Qty"
Cohesion: 0.36
Nodes (3): App(), Login(), queryClient

### Community 38 - "Snapshot: PiecePackageProduct"
Cohesion: 0.14
Nodes (12): PickItemDto, Guid, PickTaskItemResponseDto, Guid, List, PickTaskResponseDto, StartPickTaskDto, Guid (+4 more)

### Community 41 - "StockTransaction"
Cohesion: 0.18
Nodes (9): IStockTransactionRepository, DateTime, Guid, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration, AppDbContext (+1 more)

### Community 42 - "PickTask"
Cohesion: 0.33
Nodes (4): Props, Props, PickTask, PickTaskItem

### Community 43 - "PutawayTask"
Cohesion: 0.14
Nodes (15): Guid, List, Task, IPutawayTaskRepository, DateTime, Guid, ICollection, PutawayTask (+7 more)

### Community 44 - "AppDbContext"
Cohesion: 0.22
Nodes (7): Guid, IdentityUser, ModelBuilder, AppDbContext, DbSet, IdentityDbContext, IdentityRole

### Community 46 - "Snapshot: Reserved/Available Qty"
Cohesion: 0.08
Nodes (13): ModelBuilder, AddProductVolumetrics1, ModelBuilder, RefactorLocationsAndAddContainers, ModelBuilder, AddOrdersAndOrderItems, ModelBuilder, AddContainersStock (+5 more)

### Community 47 - ".AllocateOrderAsync"
Cohesion: 0.20
Nodes (14): isSupervisorAuthError(), confirmPutawayItem(), ContainerValidation, fetchActivePutawayTask(), reportPutawayMissing(), startPutaway(), validateContainer(), Props (+6 more)

### Community 50 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 53 - "Warehouse.Api.Common"
Cohesion: 0.08
Nodes (16): ResultExtensions, ConcurrencyConflictException, SplitAndCloseDto, IConfiguration, DependencyInjection, Warehouse.Api.Common, Warehouse.Application.Common, Warehouse.Infrastructure.Repositories (+8 more)

### Community 55 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 60 - "PutawayTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.17
Nodes (13): Dictionary, Guid, List, Task, IStockRepository, Guid, Stock, AppDbContext (+5 more)

### Community 62 - "AddReservedAvailableQuantity"
Cohesion: 0.18
Nodes (12): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IEnumerable, List, Task (+4 more)

### Community 64 - "RegisterDto.cs"
Cohesion: 0.08
Nodes (27): Dictionary, Guid, IEnumerable, List, Task, ILocationRepository, Guid, IsAllocated (+19 more)

### Community 67 - "20260801193412_AddOrderItemPendingReplenishment.Designer.cs"
Cohesion: 0.33
Nodes (5): ReportMissingItemDto, Fact, Mock, Task, PickTaskServiceTests

### Community 69 - "OrderAllocationService.cs"
Cohesion: 0.60
Nodes (3): Guid, Task, OrderService

### Community 70 - "Stock"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 76 - "PickTaskItemResponseDto"
Cohesion: 0.28
Nodes (8): axiosClient, extractErrorMessage(), getAvailableQuantity(), inputStyle, InventoryAdmin(), labelStyle, Product, ProductStock

### Community 77 - "20260618193047_AddProductVolumetrics.Designer.cs"
Cohesion: 0.48
Nodes (3): Guid, Task, IOrderService

### Community 81 - "Product"
Cohesion: 0.11
Nodes (19): DateTime, Dictionary, Guid, List, Task, IProductRepository, DateTime, Guid (+11 more)

## Knowledge Gaps
- **170 isolated node(s):** `$schema`, `commandName`, `dotnetRunMessages`, `launchBrowser`, `applicationUrl` (+165 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **40 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Infrastructure` connect `Snapshot: Reserved/Available Qty` to `20260809235825_Add3.Designer.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `20260618193047_AddProductVolumetrics.Designer.cs`, `20260624095509_AddIdentityTables.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `AppDbContext`, `AdjustStockDto.cs`, `20260809183404_Add2.Designer.cs`, `20260809211304_RemoveLocationFromPutawayItem.Designer.cs`, `Product & Stock Entities`, `20260618193047_AddProductVolumetrics.Designer.cs`, `InitialCreate`, `Warehouse.Api.Common`, `20260624095509_AddIdentityTables.Designer.cs`, `20260809235604_AddContainerBarcodeUniqueIndex.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `.AllocateOrderAsync`, `20260614131504_AddProductUpdatedAt.Designer.cs`?**
  _High betweenness centrality (0.205) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `Product`, `DI Composition Root`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `AddContainersStock`, `Add2`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `.AllocateOrderAsync`, `Add1`, `20260614131504_AddProductUpdatedAt.Designer.cs`, `AddConcurrencyAndStockConstraints`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260618193047_AddProductVolumetrics.Designer.cs`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260624095509_AddIdentityTables.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `20260801201234_Add1.Designer.cs`, `AdjustStockDto.cs`, `20260809183404_Add2.Designer.cs`, `20260809211304_RemoveLocationFromPutawayItem.Designer.cs`, `AddContainerBarcodeUniqueIndex`, `20260618193047_AddProductVolumetrics.Designer.cs`, `InitialCreate`, `20260624095509_AddIdentityTables.Designer.cs`, `20260805012255_AddStockTransactionJournal.Designer.cs`, `20260809235604_AddContainerBarcodeUniqueIndex.Designer.cs`, `20260809235825_Add3.Designer.cs`?**
  _High betweenness centrality (0.136) - this node is a cross-community bridge._
- **Why does `Warehouse.Domain` connect `Warehouse.Api.Common` to `RegisterDto.cs`, `IEntityTypeConfiguration`, `Orders API & Allocation`, `Containers API & Mapping`, `Stock`, `PickTaskConfiguration`, `PutawayTaskConfiguration`, `StockTransaction`, `PutawayTask`, `AppDbContext`, `PickTask Entity Mapping`, `Product`, `PickTaskItem`, `PutawayTaskConfiguration`, `PutawayTaskItem`, `Stock`, `AddReservedAvailableQuantity`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **What connects `$schema`, `commandName`, `dotnetRunMessages` to the rest of the system?**
  _170 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.12615384615384614 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._
- **Should `Frontend Lint & Axios Client` be split into smaller, more focused modules?**
  _Cohesion score 0.13105413105413105 - nodes in this community are weakly interconnected._