# Graph Report - WMS  (2026-08-09)

## Corpus Check
- 184 files · ~44,191 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1197 nodes · 2035 edges · 88 communities (55 shown, 33 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 40 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `70f6d13d`
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
- StockConfiguration
- Migration: Reserved/Available Qty
- Snapshot: InitialCreate
- AddContainersStock
- Snapshot: PiecePackageProduct
- XminConcurrencyExtensions.cs
- Snapshot: ProductVolumetrics1
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
- PutawayServiceTests
- PutawayTaskItem
- Stock
- AddReservedAvailableQuantity
- AddConcurrencyAndStockConstraints
- RegisterDto.cs
- 20260618193047_AddProductVolumetrics.Designer.cs
- DateTime
- 20260801193412_AddOrderItemPendingReplenishment.Designer.cs
- Order
- OrderAllocationService.cs
- Stock
- 20260804092556_AddPutawayTables.Designer.cs
- AddStockTransactionJournal
- PickTask
- 20260615013749_AddPiecePackageProduct.Designer.cs
- Location
- PickTaskItemResponseDto
- 20260618193047_AddProductVolumetrics.Designer.cs
- AdjustStockDto.cs
- react
- ContainerMoveDto.cs
- Product
- LoginDto.cs
- OrderCreateDto.cs
- InitialCreate
- 20260801201234_Add1.Designer.cs
- IActionResult
- IConfiguration

## God Nodes (most connected - your core abstractions)
1. `Warehouse.Application.DTOs` - 39 edges
2. `Warehouse.Infrastructure.Migrations` - 39 edges
3. `Warehouse.Domain` - 32 edges
4. `Warehouse.Application.Interfaces` - 31 edges
5. `Warehouse.Domain` - 26 edges
6. `Result` - 25 edges
7. `PutawayTask` - 25 edges
8. `Warehouse.Infrastructure` - 23 edges
9. `AppDbContext` - 18 edges
10. `Warehouse.Application.Services` - 17 edges

## Surprising Connections (you probably didn't know these)
- `IUnitOfWork` --references--> `IContainerRepository`  [EXTRACTED]
  Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs → Backend/Warehouse.Application/Interfaces/IContainerRepository.cs
- `UnitOfWork` --references--> `IContainerRepository`  [EXTRACTED]
  Backend/Warehouse.Infrastructure/Repositories/UnitOfWork.cs → Backend/Warehouse.Application/Interfaces/IContainerRepository.cs
- `IUnitOfWork` --references--> `ILocationRepository`  [EXTRACTED]
  Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs → Backend/Warehouse.Application/Interfaces/ILocationRepository.cs
- `UnitOfWork` --references--> `ILocationRepository`  [EXTRACTED]
  Backend/Warehouse.Infrastructure/Repositories/UnitOfWork.cs → Backend/Warehouse.Application/Interfaces/ILocationRepository.cs
- `IUnitOfWork` --references--> `IOrderRepository`  [EXTRACTED]
  Backend/Warehouse.Application/Interfaces/IUnitOfWork.cs → Backend/Warehouse.Application/Interfaces/IOrderRepository.cs

## Import Cycles
- None detected.

## Communities (88 total, 33 thin omitted)

### Community 0 - "PickTask API & Service"
Cohesion: 0.07
Nodes (32): ActionResult, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, IEnumerable (+24 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.06
Nodes (33): dependencies, axios, react, react-dom, react-router-dom, devDependencies, oxlint, @types/node (+25 more)

### Community 2 - "Products & Stocks API"
Cohesion: 0.10
Nodes (17): ActionResult, HttpPost, Task, InventoryController, Guid, AdjustStockDto, CreateProductWithLocationDto, Guid (+9 more)

### Community 3 - "Frontend Lint & Axios Client"
Cohesion: 0.13
Nodes (23): axiosClient, extractErrorMessage(), PickingGenerator(), randomInt(), emptyRow(), extractErrorMessage(), PutawayGenerator(), randomContainerId() (+15 more)

### Community 4 - "Orders API & Allocation"
Cohesion: 0.18
Nodes (11): Guid, List, Order, Task, IOrderRepository, AppDbContext, Guid, List (+3 more)

### Community 5 - "Containers API & Mapping"
Cohesion: 0.15
Nodes (11): Container, Guid, List, Task, IContainerRepository, AppDbContext, Container, Guid (+3 more)

### Community 6 - "TS App Compiler Config"
Cohesion: 0.09
Nodes (22): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection, moduleResolution (+14 more)

### Community 7 - "TS Node Compiler Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 8 - "Controller Namespace Hub"
Cohesion: 0.10
Nodes (24): Guid, IEnumerable, IUnitOfWork, PickTask, ReportMissingItemDto, Task, PickTaskService, Fact (+16 more)

### Community 9 - "Auth Controller & Login"
Cohesion: 0.09
Nodes (24): Warehouse.Api, net10.0, Warehouse.Application.Tests, net10.0, Microsoft.NET.Sdk, Warehouse.Application, net10.0, Microsoft.NET.Sdk (+16 more)

### Community 10 - "Locations Controller API"
Cohesion: 0.16
Nodes (14): ActionResult, Guid, HttpPost, IActionResult, IConfiguration, IdentityUser, Task, AuthController (+6 more)

### Community 11 - "Project Build Targets"
Cohesion: 0.10
Nodes (22): ActionResult, DateTime, Guid, HttpGet, HttpPost, IEnumerable, IUnitOfWork, Product (+14 more)

### Community 12 - "API Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 14 - "IEntityTypeConfiguration"
Cohesion: 0.27
Nodes (7): Guid, ICollection, Container, ContainerStatus, ContainerType, EntityTypeBuilder, ContainerConfiguration

### Community 15 - "PickTask Entity Mapping"
Cohesion: 0.17
Nodes (11): Guid, List, PickTask, Task, IPickTaskRepository, AppDbContext, Guid, List (+3 more)

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
Cohesion: 0.06
Nodes (35): AllowAnonymous, ActionResult, Authorize, Guid, HttpGet, HttpPost, IActionResult, Task (+27 more)

### Community 23 - "Migration: PiecePackageProduct"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 24 - "Migration: InitialCreate"
Cohesion: 0.20
Nodes (13): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, IUnitOfWork, List, Location (+5 more)

### Community 25 - "Migration: ProductUpdatedAt"
Cohesion: 0.18
Nodes (7): axios, AxiosRequestConfig, logout(), Props, Props, PendingFlow, Screen

### Community 28 - "Migration: Locations & Containers"
Cohesion: 0.22
Nodes (5): MigrationBuilder, RefactorLocationsAndAddContainers, MigrationBuilder, AddContainersStock, Migration

### Community 34 - "StockConfiguration"
Cohesion: 0.33
Nodes (4): Guid, Stock, EntityTypeBuilder, StockConfiguration

### Community 36 - "Snapshot: InitialCreate"
Cohesion: 0.08
Nodes (22): DateTime, Dictionary, Guid, List, Product, Task, IProductRepository, AppDbContext (+14 more)

### Community 37 - "AddContainersStock"
Cohesion: 0.28
Nodes (10): ActionResult, Container, Guid, HttpGet, HttpPost, IEnumerable, IUnitOfWork, Task (+2 more)

### Community 39 - "XminConcurrencyExtensions.cs"
Cohesion: 0.40
Nodes (3): EntityTypeBuilder, XminConcurrencyExtensions, Warehouse.Infrastructure.Configurations

### Community 40 - "Snapshot: ProductVolumetrics1"
Cohesion: 0.25
Nodes (11): ActionResult, Guid, HttpGet, HttpPost, IEnumerable, IUnitOfWork, Order, Task (+3 more)

### Community 41 - "StockTransaction"
Cohesion: 0.16
Nodes (10): IStockTransactionRepository, DateTime, Guid, StockTransaction, StockTransactionType, EntityTypeBuilder, StockTransactionConfiguration, AppDbContext (+2 more)

### Community 42 - "PickTask"
Cohesion: 0.24
Nodes (8): fetchSupervisorAuthHeader(), isSupervisorAuthError(), Props, Props, PickTasks(), Props, PickTask, PickTaskItem

### Community 43 - "PutawayTask"
Cohesion: 0.12
Nodes (17): Guid, List, Task, IPutawayTaskRepository, DateTime, Guid, ICollection, PutawayTask (+9 more)

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
Cohesion: 0.06
Nodes (24): ResultExtensions, ConcurrencyConflictException, ContainerMoveDto, LocationCreateDto, Guid, LocationResponseDto, ProductCreateDto, SplitAndCloseDto (+16 more)

### Community 55 - "PickTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PickTaskItem, EntityTypeBuilder, PickTaskItemConfiguration

### Community 57 - ".AllocateOrderAsync"
Cohesion: 0.15
Nodes (10): Guid, IsAllocated, Message, Task, IOrderAllocationService, Guid, IsAllocated, Message (+2 more)

### Community 59 - "PutawayServiceTests"
Cohesion: 0.32
Nodes (7): Fact, Mock, Task, PutawayServiceTests, ContainerStatus, PutawayService, PutawayTask

### Community 60 - "PutawayTaskItem"
Cohesion: 0.33
Nodes (4): Guid, PutawayTaskItem, EntityTypeBuilder, PutawayTaskItemConfiguration

### Community 61 - "Stock"
Cohesion: 0.20
Nodes (11): Guid, List, Stock, Task, IStockRepository, AppDbContext, Guid, List (+3 more)

### Community 64 - "RegisterDto.cs"
Cohesion: 0.13
Nodes (15): Dictionary, Guid, IEnumerable, List, Location, Task, ILocationRepository, AppDbContext (+7 more)

### Community 68 - "Order"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, Order, OrderStatus, EntityTypeBuilder, OrderConfiguration

### Community 70 - "Stock"
Cohesion: 0.33
Nodes (4): Guid, OrderItem, EntityTypeBuilder, OrderItemConfiguration

### Community 73 - "PickTask"
Cohesion: 0.24
Nodes (7): DateTime, Guid, ICollection, PickTask, PickTaskStatus, EntityTypeBuilder, PickTaskConfiguration

### Community 75 - "Location"
Cohesion: 0.28
Nodes (6): Guid, ICollection, Location, LocationType, EntityTypeBuilder, LocationConfiguration

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
Cohesion: 0.38
Nodes (6): DateTime, Guid, ICollection, Product, ProductSize, UnitType

### Community 83 - "OrderCreateDto.cs"
Cohesion: 0.50
Nodes (4): Guid, List, OrderCreateDto, OrderItemCreateDto

## Knowledge Gaps
- **167 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)`, `Swashbuckle.AspNetCore (6.6.2)`, `Microsoft.OpenApi (1.6.14)` (+162 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **33 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Warehouse.Application.Interfaces` connect `Warehouse.Api.Common` to `TestOrderGenerator/src/App.tsx`?**
  _High betweenness centrality (0.194) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure` connect `Snapshot: Reserved/Available Qty` to `20260618193047_AddProductVolumetrics.Designer.cs`, `Migration: Reserved/Available Qty`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `Snapshot: PiecePackageProduct`, `20260804092556_AddPutawayTables.Designer.cs`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `AppDbContext`, `EF Model Snapshot`, `Product & Stock Entities`, `ContainerMoveDto.cs`, `LoginDto.cs`, `20260624095509_AddIdentityTables.Designer.cs`, `Warehouse.Api.Common`, `20260801201234_Add1.Designer.cs`?**
  _High betweenness centrality (0.188) - this node is a cross-community bridge._
- **Why does `Warehouse.Infrastructure.Migrations` connect `Product & Stock Entities` to `EF Model Snapshot`, `Product`, `DI Composition Root`, `Migration: ProductVolumetrics`, `Migration: ProductVolumetrics1`, `Migration: Locations & Containers`, `Migration: Orders & OrderItems`, `Migration: Orders Constraints`, `Migration: PickTasks & Items`, `Migration: PickTasks & Items 1`, `Migration: Identity Tables`, `Migration: Reserved/Available Qty`, `Snapshot: PiecePackageProduct`, `PickTaskItem`, `Snapshot: Reserved/Available Qty`, `20260624095509_AddIdentityTables.Designer.cs`, `Add1`, `AddReservedAvailableQuantity`, `AddConcurrencyAndStockConstraints`, `20260618193047_AddProductVolumetrics.Designer.cs`, `DateTime`, `20260801193412_AddOrderItemPendingReplenishment.Designer.cs`, `OrderAllocationService.cs`, `20260804092556_AddPutawayTables.Designer.cs`, `AddStockTransactionJournal`, `20260615013749_AddPiecePackageProduct.Designer.cs`, `ContainerMoveDto.cs`, `LoginDto.cs`, `InitialCreate`, `20260801201234_Add1.Designer.cs`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **What connects `net10.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)` to the rest of the system?**
  _167 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PickTask API & Service` be split into smaller, more focused modules?**
  _Cohesion score 0.0679563492063492 - nodes in this community are weakly interconnected._
- **Should `Frontend NPM Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Products & Stocks API` be split into smaller, more focused modules?**
  _Cohesion score 0.09686609686609686 - nodes in this community are weakly interconnected._