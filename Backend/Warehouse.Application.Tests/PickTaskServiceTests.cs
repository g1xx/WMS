using FluentAssertions;
using Moq;
using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class PickTaskServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPickTaskRepository> _pickTaskRepositoryMock;
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IContainerRepository> _containerRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock;
    private readonly PickTaskService _sut;

    public PickTaskServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pickTaskRepositoryMock = new Mock<IPickTaskRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _containerRepositoryMock = new Mock<IContainerRepository>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _stockTransactionRepositoryMock = new Mock<IStockTransactionRepository>();

        _unitOfWorkMock.Setup(u => u.PickTasks).Returns(_pickTaskRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        // DispatchContainerAsync/ReportDefectAsync/ReportMissingItemAsync/PickItemAsync all
        // run their work inside a transaction; default every overload to transparently
        // running the action, same as the real UnitOfWork does on success, so most tests
        // don't need to set this up individually.
        // DispatchContainerAsync's transaction is Result<Guid?>, not a bare Guid?, so the
        // container-transition guard's rejection can travel out cleanly (see the commit
        // that fixed InProgress->Ready instead of ->Available). A different closed
        // generic from the plain Guid? one above, so it needs its own stub — Moq's
        // default for an unstubbed Task<T>-returning method silently skips the action
        // entirely rather than throwing (the same gotcha noted in PutawayServiceTests).
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Result<Guid?>>>>()))
            .Returns<Func<Task<Result<Guid?>>>>(action => action());
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid?>>>()))
            .Returns<Func<Task<Guid?>>>(action => action());
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ReportDefectResultDto>>>()))
            .Returns<Func<Task<ReportDefectResultDto>>>(action => action());
        // ReportMissingItemAsync's transaction returns the built message string.
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<string>>>()))
            .Returns<Func<Task<string>>>(action => action());
        // PickItemAsync's transaction has no return value — the non-generic overload.
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());
        // GetNextTaskAsync's transaction returns the claimed task. Another distinct closed
        // generic, and per the gotcha above an unstubbed one would silently skip the action
        // and make every claim test pass for the wrong reason (null == "no work available").
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<PickTask?>>>()))
            .Returns<Func<Task<PickTask?>>>(action => action());

        // ReportMissingItemAsync now always runs a replacement search (via
        // IUnfulfillableUnitHandler) same as ReportDefectAsync always has. Default to
        // "nothing found" so tests that aren't about replacement sourcing specifically
        // don't all need to stub this themselves — same reasoning as the
        // ExecuteInTransactionAsync defaults above.
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Stock>());

        // Defaults so tests that don't specifically exercise a container transition don't
        // need to set these up individually: no-op lock (returns null = "not found",
        // which every real container-bearing test overrides with its own container's
        // actual status).
        _containerRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ContainerStatus?)null);

        // Real implementations, not mocks: all four are pure logic with no dependencies
        // of their own beyond the (already-mocked) IUnitOfWork, and these tests don't care
        // about item order or replacement zone selection beyond what's asserted explicitly.
        _sut = new PickTaskService(
            _unitOfWorkMock.Object,
            new RouteOptimizerService(),
            new UnfulfillableUnitHandler(_unitOfWorkMock.Object, new DefectReplacementPlanner()),
            new ContainerLifecycleService(_unitOfWorkMock.Object),
            new PickTaskSettings());
    }

    // Builds a single-item InProgress task: required 10, picked 0, missing 0,
    // located/skus matching the dto used by the ReportMissingItemAsync tests below.
    private static PickTask BuildTaskWithOneItem(string assignedWorkerId = "worker-1", int requiredQuantity = 10, int pickedQuantity = 0)
    {
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        var product = new Product { Id = Guid.NewGuid(), Sku = "SKU-1" };

        return new PickTask
        {
            Id = Guid.NewGuid(),
            Status = PickTaskStatus.InProgress,
            AssignedWorkerId = assignedWorkerId,
            Items = new List<PickTaskItem>
            {
                new()
                {
                    ProductId = product.Id,
                    Product = product,
                    LocationId = location.Id,
                    Location = location,
                    RequiredQuantity = requiredQuantity,
                    PickedQuantity = pickedQuantity,
                    MissingQuantity = 0
                }
            }
        };
    }

    private static ReportMissingItemDto BuildMissingItemDto(int missingQuantity) => new()
    {
        LocationBarcode = "LOC-1",
        ProductSku = "SKU-1",
        MissingQuantity = missingQuantity
    };

    // ===================== ReportMissingItemAsync =====================

    [Fact]
    public async Task ReportMissingItemAsync_CallerIsNotAssignedWorker_StillSucceeds()
    {
        // Arrange: this endpoint is gated by Brigadier/Admin RBAC at the controller,
        // not by task ownership — the service itself must not re-block on a mismatch.
        var task = BuildTaskWithOneItem(assignedWorkerId: "picker-1");
        var stock = new Stock { ProductId = task.Items.First().ProductId, LocationId = task.Items.First().LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(stock.ProductId, stock.LocationId)).ReturnsAsync(stock);

        var dto = BuildMissingItemDto(2);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-2");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReportMissingItemAsync_ValidReport_AdjustsStockAndLogsTransaction()
    {
        // Arrange
        var task = BuildTaskWithOneItem();
        var taskItem = task.Items.First();
        var stock = new Stock
        {
            ProductId = taskItem.ProductId,
            LocationId = taskItem.LocationId,
            PhysicalQuantity = 10,
            ReservedQuantity = 10
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(stock);

        var dto = BuildMissingItemDto(3);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        taskItem.MissingQuantity.Should().Be(3);
        stock.PhysicalQuantity.Should().Be(7);
        stock.ReservedQuantity.Should().Be(7);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == taskItem.ProductId &&
            t.LocationId == taskItem.LocationId &&
            t.QuantityChange == -3 &&
            t.TransactionType == StockTransactionType.Missing)), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReportMissingItemAsync_AllItemsFullyAccountedFor_DoesNotCompleteTask()
    {
        // Arrange: one item, required 10, already picked 7 — reporting the last 3 as
        // missing means every line is now accounted for (picked or missing). The task
        // must NOT auto-complete here: the worker still has to physically dispatch the
        // container onto the conveyor, and DispatchContainerAsync is the only place
        // that releases the container and moves stock for what was actually picked.
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 7);
        var taskItem = task.Items.First();
        var stock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 3, ReservedQuantity = 3 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(stock);

        var dto = BuildMissingItemDto(3);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PickTaskStatus.InProgress);
    }

    [Fact]
    public async Task ReportMissingItemAsync_MissingExceedsOutstanding_ReturnsFailure()
    {
        // Arrange: required 10, already picked 7 — only 3 units are legitimately
        // outstanding, so reporting 4 as missing must be rejected rather than silently
        // overshooting (which would corrupt the leftover/order-writeoff math downstream).
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 7);
        var taskItem = task.Items.First();

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);

        var dto = BuildMissingItemDto(4);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        taskItem.MissingQuantity.Should().Be(0);
    }

    [Fact]
    public async Task ReportMissingItemAsync_ZeroOrNegativeQuantity_ReturnsFailure()
    {
        var task = BuildTaskWithOneItem();

        var dto = BuildMissingItemDto(0);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ReportMissingItemAsync_NoReplacementFound_WritesOffOrderItemShortedQuantity()
    {
        // Arrange: RequiredQuantity must stay untouched — it always reflects what was
        // actually ordered. With no replacement stock anywhere (default stub), the
        // shortfall lands on ShortedQuantity instead, and the line is flagged for
        // replenishment, exactly like an unrecoverable defect would be.
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 7);
        var taskItem = task.Items.First();
        var stock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 3, ReservedQuantity = 3 };

        var order = new Order
        {
            Id = task.OrderId,
            Items = new List<OrderItem>
            {
                new() { ProductId = taskItem.ProductId, RequiredQuantity = 10, PickedQuantity = 7 }
            }
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(stock);
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);

        var dto = BuildMissingItemDto(3);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = order.Items.First();
        orderItem.RequiredQuantity.Should().Be(10, "history is never rewritten — this is still what was ordered");
        orderItem.PickedQuantity.Should().Be(7);
        orderItem.ShortedQuantity.Should().Be(3);
        orderItem.IsPendingReplenishment.Should().BeTrue();
    }

    [Fact]
    public async Task ReportMissingItemAsync_ReplacementFoundInActiveZone_CoversShortfallWithoutTouchingOrder()
    {
        // Arrange: a genuinely missing unit gets the same chance a defective one does —
        // if a replacement is sitting in another active picking zone, take it instead of
        // writing the order off.
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 0);
        task.Sector = "mp1";
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        var replacementLocation = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-2", WarehouseCode = "m", Sector = "r", Floor = 1 };
        var replacementStock = new Stock
        {
            ProductId = taskItem.ProductId,
            LocationId = replacementLocation.Id,
            Location = replacementLocation,
            PhysicalQuantity = 10,
            ReservedQuantity = 0
        };

        var order = new Order
        {
            Id = task.OrderId,
            Items = new List<OrderItem> { new() { ProductId = taskItem.ProductId, RequiredQuantity = 10, PickedQuantity = 0 } }
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(taskItem.ProductId, taskItem.LocationId, It.IsAny<string>()))
            .ReturnsAsync(new List<Stock> { replacementStock });
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);

        var dto = BuildMissingItemDto(10);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = order.Items.First();
        orderItem.ShortedQuantity.Should().Be(0);
        orderItem.IsPendingReplenishment.Should().BeFalse();
        replacementStock.ReservedQuantity.Should().Be(10);
        // Different zone (mr1) from the task's own sector (mp1) -> a new PickTask, not an
        // appended line on the current one.
        _pickTaskRepositoryMock.Verify(r => r.Add(It.Is<PickTask>(t => t.Sector == "mr1" && t.OrderId == task.OrderId)), Times.Once);
    }

    [Fact]
    public async Task ReportMissingItemAsync_ReplacementOnlyInBulkSector_IsIgnoredAndShortsTheOrder()
    {
        // Arrange: plenty of physical stock exists, but only in the bulk/reserve sector
        // ("w") — a picker can't be routed there, so this must be treated exactly like no
        // stock existing at all.
        var task = BuildTaskWithOneItem(requiredQuantity: 5, pickedQuantity: 0);
        task.Sector = "mp1";
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 5, ReservedQuantity = 5 };

        var bulkLocation = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-BULK", WarehouseCode = "m", Sector = "w", Floor = 1 };
        var bulkStock = new Stock
        {
            ProductId = taskItem.ProductId,
            LocationId = bulkLocation.Id,
            Location = bulkLocation,
            PhysicalQuantity = 500,
            ReservedQuantity = 0
        };

        var order = new Order
        {
            Id = task.OrderId,
            Items = new List<OrderItem> { new() { ProductId = taskItem.ProductId, RequiredQuantity = 5, PickedQuantity = 0 } }
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(taskItem.ProductId, taskItem.LocationId, It.IsAny<string>()))
            .ReturnsAsync(new List<Stock> { bulkStock });
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);

        var dto = BuildMissingItemDto(5);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = order.Items.First();
        orderItem.ShortedQuantity.Should().Be(5, "the only stock found is in a bulk sector and must be ignored");
        orderItem.IsPendingReplenishment.Should().BeTrue();
        bulkStock.ReservedQuantity.Should().Be(0, "a bulk-sector row must never be reserved for a picker replacement");
        _pickTaskRepositoryMock.Verify(r => r.Add(It.IsAny<PickTask>()), Times.Never);
    }

    [Fact]
    public async Task ReportMissingItemAsync_OtherItemsStillUnresolved_DoesNotCompleteTask()
    {
        // Arrange: two items on the task; only the first gets fully resolved by this
        // report, the second still has required quantity outstanding.
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        var product = new Product { Id = Guid.NewGuid(), Sku = "SKU-1" };
        var resolvedItem = new PickTaskItem
        {
            ProductId = product.Id,
            Product = product,
            LocationId = location.Id,
            Location = location,
            RequiredQuantity = 10,
            PickedQuantity = 7,
            MissingQuantity = 0
        };
        var unresolvedItem = new PickTaskItem
        {
            ProductId = Guid.NewGuid(),
            Product = new Product { Id = Guid.NewGuid(), Sku = "SKU-2" },
            LocationId = Guid.NewGuid(),
            Location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-2" },
            RequiredQuantity = 5,
            PickedQuantity = 0,
            MissingQuantity = 0
        };

        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            Status = PickTaskStatus.InProgress,
            AssignedWorkerId = "worker-1",
            Items = new List<PickTaskItem> { resolvedItem, unresolvedItem }
        };

        var stock = new Stock { ProductId = resolvedItem.ProductId, LocationId = resolvedItem.LocationId, PhysicalQuantity = 3, ReservedQuantity = 3 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(resolvedItem.ProductId, resolvedItem.LocationId)).ReturnsAsync(stock);

        var dto = BuildMissingItemDto(3);

        // Act
        var result = await _sut.ReportMissingItemAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PickTaskStatus.InProgress);
    }

    // ===================== PickItemAsync =====================
    //
    // Stock must be decremented the instant an item is scanned into the tote, not
    // deferred to DispatchContainerAsync — otherwise a cycle count taken while a worker
    // is still mid-route (tote half full, container nowhere near the conveyor) would
    // see picked units as if they were still sitting on the shelf.

    [Fact]
    public async Task PickItemAsync_ValidScan_DecrementsShelfStockImmediately_BeforeAnyDispatch()
    {
        // Arrange: 10 physically on the shelf, all 10 reserved for this order.
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 0);
        var taskItem = task.Items.First();
        var stock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(stock);

        var dto = new PickItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 4 };

        // Act — pick only, no dispatch call at all.
        var result = await _sut.PickItemAsync(task.Id, dto, "worker-1");

        // Assert: the shelf already reflects reality, mid-route, with no dispatch in sight.
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(6, "a cycle count taken right now must see what's actually on the shelf");
        stock.ReservedQuantity.Should().Be(6);
        taskItem.PickedQuantity.Should().Be(4);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == taskItem.ProductId &&
            t.LocationId == taskItem.LocationId &&
            t.QuantityChange == -4 &&
            t.TransactionType == StockTransactionType.Pick)), Times.Once);
    }

    [Fact]
    public async Task PickItemAsync_QuantityExceedsPhysicalStock_ReturnsFailureWithoutMutatingAnything()
    {
        // Arrange: the task item still expects 10, but the shelf itself only has 3 left
        // (e.g. a cycle-count adjustment shrank it after allocation) — over-pick against
        // the task's own requirement would pass, but the shelf genuinely can't cover it.
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 0);
        var taskItem = task.Items.First();
        var stock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 3, ReservedQuantity = 3 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(stock);

        var dto = new PickItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 5 };

        // Act
        var result = await _sut.PickItemAsync(task.Id, dto, "worker-1");

        // Assert: clean failure, not a DB check-constraint exception, and nothing moved.
        result.IsSuccess.Should().BeFalse();
        stock.PhysicalQuantity.Should().Be(3);
        stock.ReservedQuantity.Should().Be(3);
        taskItem.PickedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task PickItemAsync_NoStockRecordAtLocation_ReturnsFailure()
    {
        var task = BuildTaskWithOneItem(requiredQuantity: 10, pickedQuantity: 0);

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);

        var dto = new PickItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 1 };

        // Act
        var result = await _sut.PickItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    // ===================== DispatchContainerAsync =====================

    [Fact]
    public async Task DispatchContainerAsync_TransactionThrows_PropagatesExceptionInsteadOfSwallowingIt()
    {
        // Arrange: a transaction failure (e.g. a concurrency conflict) must propagate
        // to the caller rather than being swallowed — the global exception handler is
        // the only thing responsible for turning it into a response.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", pickedQuantity: 1);
        task.ContainerId = Guid.NewGuid();
        var container = new Container { Id = task.ContainerId.Value, Barcode = "CONT-1", Status = ContainerStatus.InProgress };
        var station = new Location { Id = Guid.NewGuid(), AddressBarcode = "CONV-1" };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(task.ContainerId!.Value)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(task.ContainerId.Value)).ReturnsAsync(container.Status);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("CONV-1")).ReturnsAsync(station);
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Result<Guid?>>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var dto = new DispatchContainerDto { ContainerBarcode = "CONT-1", ConveyorBarcode = "CONV-1" };

        // Act
        Func<Task> act = () => _sut.DispatchContainerAsync(task.Id, dto, "worker-1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===================== ReportDefectAsync =====================

    [Fact]
    public async Task ReportDefectAsync_TransactionThrows_PropagatesExceptionInsteadOfSwallowingIt()
    {
        // Arrange: same guarantee as DispatchContainerAsync above — a transaction
        // failure here must also propagate rather than being swallowed.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1");
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ReportDefectResultDto>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var dto = new ReportDefectDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", DefectiveQuantity = 2 };

        // Act
        Func<Task> act = () => _sut.ReportDefectAsync(task.Id, dto, "supervisor-1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReportDefectAsync_ReplacementFoundInSameZone_AppendsLineToCurrentTask()
    {
        // Arrange: 3 defective units, fully replaceable from one candidate stock
        // that happens to be in the same zone as the task.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", requiredQuantity: 10, pickedQuantity: 0);
        task.Sector = "mp1";
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        var replacementLocation = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-2", WarehouseCode = "m", Sector = "p", Floor = 1 };
        var replacementStock = new Stock { ProductId = taskItem.ProductId, LocationId = replacementLocation.Id, Location = replacementLocation, PhysicalQuantity = 5, ReservedQuantity = 0 };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(taskItem.ProductId, taskItem.LocationId, "w"))
            .ReturnsAsync(new List<Stock> { replacementStock });

        var dto = new ReportDefectDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", DefectiveQuantity = 3 };

        // Act
        var result = await _sut.ReportDefectAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DefectiveQuantityDeducted.Should().Be(3);
        result.Value.AppendedToCurrentTaskQuantity.Should().Be(3);
        result.Value.ShortageQuantity.Should().Be(0);

        task.Items.Should().HaveCount(2);
        task.Items.Should().Contain(i => i.LocationId == replacementLocation.Id && i.RequiredQuantity == 3);
        replacementStock.ReservedQuantity.Should().Be(3);
        sourceStock.PhysicalQuantity.Should().Be(7);
    }

    [Fact]
    public async Task ReportDefectAsync_NoReplacementCandidates_FlagsOrderItemForReplenishment()
    {
        // Arrange: no candidate stock exists anywhere for this product, so the
        // whole replacement need becomes a shortage the order must be flagged for.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", requiredQuantity: 10, pickedQuantity: 0);
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        var orderItem = new OrderItem { ProductId = taskItem.ProductId, RequiredQuantity = 10 };
        var order = new Order { Id = task.OrderId, Items = new List<OrderItem> { orderItem } };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(taskItem.ProductId, taskItem.LocationId, "w"))
            .ReturnsAsync(new List<Stock>());
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);

        var dto = new ReportDefectDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", DefectiveQuantity = 4 };

        // Act
        var result = await _sut.ReportDefectAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ShortageQuantity.Should().Be(4);
        result.Value.AppendedToCurrentTaskQuantity.Should().Be(0);
        result.Value.NewPickTaskIds.Should().BeEmpty();
        orderItem.IsPendingReplenishment.Should().BeTrue();
    }

    [Fact]
    public async Task ReportDefectAsync_ReplacementOnlyInNonActiveZone_IsIgnoredAndFlagsReplenishment()
    {
        // Arrange: replacement stock must come from an enumerated active picking zone,
        // not merely "any zone but bulk". "mp5" (a real warehouse/sector combination,
        // just not one workers currently pick from) must be rejected even though it
        // isn't the bulk sector.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", requiredQuantity: 10, pickedQuantity: 0);
        var taskItem = task.Items.First();
        var sourceStock = new Stock { ProductId = taskItem.ProductId, LocationId = taskItem.LocationId, PhysicalQuantity = 10, ReservedQuantity = 10 };

        var nonActiveLocation = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-5", WarehouseCode = "m", Sector = "p", Floor = 5 };
        var nonActiveStock = new Stock { ProductId = taskItem.ProductId, LocationId = nonActiveLocation.Id, Location = nonActiveLocation, PhysicalQuantity = 50, ReservedQuantity = 0 };

        var orderItem = new OrderItem { ProductId = taskItem.ProductId, RequiredQuantity = 10 };
        var order = new Order { Id = task.OrderId, Items = new List<OrderItem> { orderItem } };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId)).ReturnsAsync(sourceStock);
        _stockRepositoryMock
            .Setup(r => r.GetReplacementCandidatesAsync(taskItem.ProductId, taskItem.LocationId, "w"))
            .ReturnsAsync(new List<Stock> { nonActiveStock });
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);

        var dto = new ReportDefectDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", DefectiveQuantity = 4 };

        // Act
        var result = await _sut.ReportDefectAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ShortageQuantity.Should().Be(4);
        result.Value.AppendedToCurrentTaskQuantity.Should().Be(0);
        result.Value.NewPickTaskIds.Should().BeEmpty();
        nonActiveStock.ReservedQuantity.Should().Be(0, "stock outside the active picking zones must never be reserved for a picker");
        orderItem.IsPendingReplenishment.Should().BeTrue();
    }

    // ===================== CancelPickTaskAsync =====================

    [Fact]
    public async Task CancelPickTaskAsync_ItemAlreadyPicked_RejectsCancellation()
    {
        // Arrange: one unit already picked into the container — cancelling now would
        // strand that physical stock in an unassigned container.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", pickedQuantity: 1);
        task.ContainerId = Guid.NewGuid();

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);

        // Act
        var result = await _sut.CancelPickTaskAsync(task.Id, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already been picked");
        task.Status.Should().Be(PickTaskStatus.InProgress);

        _containerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelPickTaskAsync_NothingPicked_ReleasesContainerAndResetsTask()
    {
        // Arrange: no units picked yet, so the container is still physically empty.
        var task = BuildTaskWithOneItem(assignedWorkerId: "worker-1", pickedQuantity: 0);
        var containerId = Guid.NewGuid();
        task.ContainerId = containerId;

        var container = new Container
        {
            Id = containerId,
            Status = ContainerStatus.InProgress,
            AssignedSector = "mp1"
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(containerId)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(containerId)).ReturnsAsync(container.Status);

        // Act
        var result = await _sut.CancelPickTaskAsync(task.Id, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        container.Status.Should().Be(ContainerStatus.Available);
        container.AssignedSector.Should().BeNull();

        task.Status.Should().Be(PickTaskStatus.New);
        task.AssignedWorkerId.Should().BeNull();
        task.ContainerId.Should().BeNull();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ===================== StartPickTaskAsync =====================

    [Fact]
    public async Task StartPickTaskAsync_ContainerAlreadyTaken_ReturnsConflictFromGuard()
    {
        // Arrange: another worker's request already committed InProgress on this exact
        // container in the moment between our (unlocked) barcode lookup and this call —
        // LockForUpdateAsync's fresh, tracker-bypassing read reflects that, and the
        // transition guard rejects rather than letting a second claim through.
        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            Status = PickTaskStatus.New,
            AssignedWorkerId = null,
            Sector = "mp1"
        };
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Barcode = "CONT-1",
            Status = ContainerStatus.Available
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _containerRepositoryMock.Setup(r => r.GetByBarcodeAsync("CONT-1")).ReturnsAsync(container);
        // The fresh lock-read reports a status that has already moved on from what the
        // caller's own (unlocked) container instance still shows — simulating the race.
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(ContainerStatus.InProgress);

        var dto = new StartPickTaskDto { ContainerBarcode = "CONT-1" };

        // Act
        var result = await _sut.StartPickTaskAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("currently InProgress");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task StartPickTaskAsync_TaskClaimedConcurrently_ReturnsConflictFailure()
    {
        // Arrange: the container claim itself succeeds (it's genuinely Available), but
        // saving the task's own assignment hits a concurrency conflict — e.g. another
        // request cancelled/reassigned this exact task in the same window. UnitOfWork
        // translates EF's DbUpdateConcurrencyException into ConcurrencyConflictException,
        // which the service must turn into a Result, not let bubble up as an unhandled
        // exception.
        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            Status = PickTaskStatus.New,
            AssignedWorkerId = null,
            Sector = "mp1"
        };
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Barcode = "CONT-1",
            Status = ContainerStatus.Available
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _containerRepositoryMock.Setup(r => r.GetByBarcodeAsync("CONT-1")).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(task.OrderId)).ReturnsAsync((Order?)null);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception("inner")));

        var dto = new StartPickTaskDto { ContainerBarcode = "CONT-1" };

        // Act
        var result = await _sut.StartPickTaskAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("claimed by another worker");
    }

    // ===== Alternating picked/missing items -> full Pick+Dispatch flow =====
    //
    // A missing item must not auto-complete the task before the container is
    // physically dispatched — the container has to actually reach the conveyor for
    // stock to move and for the order's shortfall to be written off, or the order can
    // never reach a terminal state. These tests drive the real worker sequence (scan
    // present items via PickItemAsync, report absent ones via ReportMissingItemAsync,
    // then physically dispatch the container) across several alternating
    // present/missing patterns to confirm the order completes cleanly every time.

    private sealed record ScenarioLine(Product Product, Location Location, Stock Stock, bool IsPresent);

    private static (Order Order, PickTask Task, Container Container, Location Station, List<ScenarioLine> Lines) BuildDispatchScenario(
        bool[] presentPattern, int quantityPerItem)
    {
        var orderId = Guid.NewGuid();
        var lines = new List<ScenarioLine>();
        var taskItems = new List<PickTaskItem>();
        var orderItems = new List<OrderItem>();

        for (var i = 0; i < presentPattern.Length; i++)
        {
            var product = new Product { Id = Guid.NewGuid(), Sku = $"SKU-{i}" };
            var location = new Location { Id = Guid.NewGuid(), AddressBarcode = $"LOC-{i}" };
            var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = quantityPerItem, ReservedQuantity = quantityPerItem };

            taskItems.Add(new PickTaskItem
            {
                ProductId = product.Id,
                Product = product,
                LocationId = location.Id,
                Location = location,
                RequiredQuantity = quantityPerItem,
                PickedQuantity = 0,
                MissingQuantity = 0
            });
            orderItems.Add(new OrderItem { ProductId = product.Id, RequiredQuantity = quantityPerItem, PickedQuantity = 0 });

            lines.Add(new ScenarioLine(product, location, stock, presentPattern[i]));
        }

        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PickTaskStatus.InProgress,
            AssignedWorkerId = "worker-1",
            Sector = "mp1",
            ContainerId = Guid.NewGuid(),
            Items = taskItems
        };

        var order = new Order { Id = orderId, Status = OrderStatus.Picking, Items = orderItems };
        var container = new Container { Id = task.ContainerId!.Value, Barcode = "CONT-1", Status = ContainerStatus.InProgress };
        var station = new Location { Id = Guid.NewGuid(), AddressBarcode = "CONV-1" };

        return (order, task, container, station, lines);
    }

    private async Task<Result<DispatchContainerResultDto>> RunAlternatingScenarioAsync(bool[] presentPattern, int quantityPerItem = 10)
    {
        var (order, task, container, station, lines) = BuildDispatchScenario(presentPattern, quantityPerItem);

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(station.AddressBarcode)).ReturnsAsync(station);

        foreach (var line in lines)
        {
            _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(line.Product.Id, line.Location.Id)).ReturnsAsync(line.Stock);
        }

        foreach (var line in lines)
        {
            if (line.IsPresent)
            {
                var pickDto = new PickItemDto { LocationBarcode = line.Location.AddressBarcode, ProductSku = line.Product.Sku, Quantity = quantityPerItem };
                var pickResult = await _sut.PickItemAsync(task.Id, pickDto, "worker-1");
                pickResult.IsSuccess.Should().BeTrue($"picking {line.Product.Sku} should succeed: {pickResult.Error}");
            }
            else
            {
                var missingDto = new ReportMissingItemDto { LocationBarcode = line.Location.AddressBarcode, ProductSku = line.Product.Sku, MissingQuantity = quantityPerItem };
                var missingResult = await _sut.ReportMissingItemAsync(task.Id, missingDto, "supervisor-1");
                missingResult.IsSuccess.Should().BeTrue($"reporting {line.Product.Sku} missing should succeed: {missingResult.Error}");
            }
        }

        // The task must still be InProgress right up to the moment of physical dispatch,
        // regardless of how many items were reported missing along the way.
        task.Status.Should().Be(PickTaskStatus.InProgress);

        var dispatchDto = new DispatchContainerDto { ContainerBarcode = "CONT-1", ConveyorBarcode = "CONV-1" };
        var dispatchResult = await _sut.DispatchContainerAsync(task.Id, dispatchDto, "worker-1");

        dispatchResult.IsSuccess.Should().BeTrue($"dispatch should succeed: {dispatchResult.Error}");
        task.Status.Should().Be(PickTaskStatus.Completed);
        // Staged on the conveyor, still physically loaded — Ready, not Available. This is
        // the actual bug: marking it Available here is what let a second worker claim an
        // in-use container.
        container.Status.Should().Be(ContainerStatus.Ready);

        // Every line was either fully picked or written off as missing, so the order
        // always reaches SOME terminal state — but the two outcomes must stay
        // distinguishable: only an all-picked pattern reaches Packed. Any pattern with
        // at least one missing line (and, per the default stub, no replacement found
        // anywhere) must land on ShortShipped instead, never silently as Packed.
        var expectedStatus = presentPattern.All(isPresent => isPresent) ? OrderStatus.Packed : OrderStatus.ShortShipped;
        order.Status.Should().Be(expectedStatus);

        for (var i = 0; i < lines.Count; i++)
        {
            var orderItem = order.Items.First(oi => oi.ProductId == lines[i].Product.Id);
            orderItem.RequiredQuantity.Should().Be(quantityPerItem, "RequiredQuantity must never be rewritten");
            if (lines[i].IsPresent)
            {
                orderItem.ShortedQuantity.Should().Be(0);
            }
            else
            {
                orderItem.ShortedQuantity.Should().Be(quantityPerItem, "no replacement stock exists anywhere in this scenario");
                orderItem.IsPendingReplenishment.Should().BeTrue();
            }
        }

        dispatchResult.Value!.NextTaskId.Should().BeNull("every line was accounted for, so no leftover pick task should be spawned");
        _pickTaskRepositoryMock.Verify(r => r.Add(It.IsAny<PickTask>()), Times.Never);

        return dispatchResult;
    }

    public static IEnumerable<object[]> AlternatingPatterns()
    {
        // [Present, Missing, Present] — the exact scenario requested.
        yield return new object[] { new[] { true, false, true } };
        // Missing first and last instead.
        yield return new object[] { new[] { false, true, false } };
        // Longer alternating run: "...and so on".
        yield return new object[] { new[] { true, false, true, false, true } };
        yield return new object[] { new[] { false, true, false, true, false } };
        // Two missing items in a row in the middle of an otherwise-normal task.
        yield return new object[] { new[] { true, false, false, true } };
        // Sanity check: no missing items at all still behaves as before.
        yield return new object[] { new[] { true, true, true } };
    }

    [Theory]
    [MemberData(nameof(AlternatingPatterns))]
    public async Task DispatchContainerAsync_AlternatingPickedAndMissingItems_OrderReachesTerminalState(bool[] presentPattern)
    {
        await RunAlternatingScenarioAsync(presentPattern);
    }

    // Runs the exact "item 1 in stock, item 2 missing, item 3 in stock" scenario several
    // times over to confirm the outcome isn't order-of-operations- or Moq-call-ordering-
    // sensitive — each run builds entirely fresh entities/mocks.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task DispatchContainerAsync_CanonicalStockMissingStockPattern_SucceedsOnRepeatedRuns(int run)
    {
        // Vary quantity per run too, so "several times" isn't just the identical call five times.
        await RunAlternatingScenarioAsync(new[] { true, false, true }, quantityPerItem: run * 3 + 1);
    }

    [Fact]
    public async Task DispatchContainerAsync_AlternatingPickedAndMissingItems_StockAndTransactionsAreCorrect()
    {
        var (order, task, container, station, lines) = BuildDispatchScenario(new[] { true, false, true }, quantityPerItem: 10);

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(station.AddressBarcode)).ReturnsAsync(station);

        foreach (var line in lines)
        {
            _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(line.Product.Id, line.Location.Id)).ReturnsAsync(line.Stock);
        }

        await _sut.PickItemAsync(task.Id, new PickItemDto { LocationBarcode = lines[0].Location.AddressBarcode, ProductSku = lines[0].Product.Sku, Quantity = 10 }, "worker-1");
        await _sut.ReportMissingItemAsync(task.Id, new ReportMissingItemDto { LocationBarcode = lines[1].Location.AddressBarcode, ProductSku = lines[1].Product.Sku, MissingQuantity = 10 }, "supervisor-1");
        await _sut.PickItemAsync(task.Id, new PickItemDto { LocationBarcode = lines[2].Location.AddressBarcode, ProductSku = lines[2].Product.Sku, Quantity = 10 }, "worker-1");

        var dispatchResult = await _sut.DispatchContainerAsync(task.Id, new DispatchContainerDto { ContainerBarcode = "CONT-1", ConveyorBarcode = "CONV-1" }, "worker-1");

        dispatchResult.IsSuccess.Should().BeTrue();

        // Item 1 (present): physically picked and dispatched — stock fully drawn down.
        lines[0].Stock.PhysicalQuantity.Should().Be(0);
        lines[0].Stock.ReservedQuantity.Should().Be(0);

        // Item 2 (missing): written off entirely at report-missing time, untouched by dispatch.
        lines[1].Stock.PhysicalQuantity.Should().Be(0);
        lines[1].Stock.ReservedQuantity.Should().Be(0);

        // Item 3 (present): same as item 1.
        lines[2].Stock.PhysicalQuantity.Should().Be(0);
        lines[2].Stock.ReservedQuantity.Should().Be(0);

        // RequiredQuantity is never rewritten for any line — it always reflects what was
        // actually ordered. Item 2's shortfall is recorded on ShortedQuantity instead,
        // and the order lands on ShortShipped, not silently on Packed.
        var orderItem0 = order.Items.First(oi => oi.ProductId == lines[0].Product.Id);
        var orderItem1 = order.Items.First(oi => oi.ProductId == lines[1].Product.Id);
        var orderItem2 = order.Items.First(oi => oi.ProductId == lines[2].Product.Id);

        orderItem0.RequiredQuantity.Should().Be(10);
        orderItem0.ShortedQuantity.Should().Be(0);

        orderItem1.RequiredQuantity.Should().Be(10);
        orderItem1.ShortedQuantity.Should().Be(10);
        orderItem1.IsPendingReplenishment.Should().BeTrue();

        orderItem2.RequiredQuantity.Should().Be(10);
        orderItem2.ShortedQuantity.Should().Be(0);

        order.Status.Should().Be(OrderStatus.ShortShipped);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == lines[0].Product.Id && t.TransactionType == StockTransactionType.Pick && t.QuantityChange == -10)), Times.Once);
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == lines[1].Product.Id && t.TransactionType == StockTransactionType.Missing && t.QuantityChange == -10)), Times.Once);
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == lines[2].Product.Id && t.TransactionType == StockTransactionType.Pick && t.QuantityChange == -10)), Times.Once);
    }

    // ---- Closing an empty container -------------------------------------------------
    // "Full container" must be refused on a container nothing was picked into, but the
    // refusal keys on outstanding WORK, not on emptiness alone: a task whose lines were
    // all written off is also empty, yet must still close or its order never leaves Picking.

    [Fact]
    public async Task DispatchContainerAsync_NothingPickedAndWorkOutstanding_IsRejected()
    {
        var (order, task, container, station, _) = BuildDispatchScenario(new[] { true }, quantityPerItem: 10);

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(station.AddressBarcode)).ReturnsAsync(station);

        // Nothing picked, nothing written off — the line is still real outstanding work.
        var result = await _sut.DispatchContainerAsync(
            task.Id, new DispatchContainerDto { ContainerBarcode = "CONT-1", ConveyorBarcode = "CONV-1" }, "worker-1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty");

        // Rejected before any state moved: the task is still the worker's to finish.
        task.Status.Should().Be(PickTaskStatus.InProgress);
        container.Status.Should().Be(ContainerStatus.InProgress);
        order.Status.Should().Be(OrderStatus.Picking);
    }

    [Fact]
    public async Task DispatchContainerAsync_AllLinesWrittenOff_ClosesOutAndFreesTheEmptyContainer()
    {
        var (order, task, container, station, lines) = BuildDispatchScenario(new[] { false, false }, quantityPerItem: 10);

        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAndProductLocationAsync(task.Id)).ReturnsAsync(task);
        _pickTaskRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.Id)).ReturnsAsync(task);
        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(task.OrderId)).ReturnsAsync(order);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(station.AddressBarcode)).ReturnsAsync(station);

        foreach (var line in lines)
        {
            _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(line.Product.Id, line.Location.Id)).ReturnsAsync(line.Stock);
            await _sut.ReportMissingItemAsync(
                task.Id,
                new ReportMissingItemDto { LocationBarcode = line.Location.AddressBarcode, ProductSku = line.Product.Sku, MissingQuantity = 10 },
                "supervisor-1");
        }

        var result = await _sut.DispatchContainerAsync(
            task.Id, new DispatchContainerDto { ContainerBarcode = "CONT-1", ConveyorBarcode = "CONV-1" }, "worker-1");

        result.IsSuccess.Should().BeTrue($"a fully written-off task must still close: {result.Error}");
        task.Status.Should().Be(PickTaskStatus.Completed);

        // The whole point of allowing this path: the order's shortfall is fully recorded,
        // so it must reach ShortShipped. Routing this to cancel instead would strand it
        // in Picking forever.
        order.Status.Should().Be(OrderStatus.ShortShipped);

        // Physically empty and it never went to the conveyor, so it goes straight back to
        // the free pool — NOT Ready, which would block it awaiting a putaway that has no
        // goods to put away and will never be created.
        container.Status.Should().Be(ContainerTransitions.FreeStatus);
        container.LocationId.Should().NotBe(station.Id, "an empty container never travelled to the conveyor");
        container.AssignedSector.Should().BeNull();

        result.Value!.NextTaskId.Should().BeNull("every line was accounted for");
    }

    // ---- Claiming a task at show-time ------------------------------------------------

    [Fact]
    public async Task GetNextTaskAsync_ClaimsTheTaskForTheWorkerWhileLeavingItNew()
    {
        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            Sector = "mp1",
            Status = PickTaskStatus.New,
            Items = new List<PickTaskItem>()
        };

        _pickTaskRepositoryMock
            .Setup(r => r.ClaimNextForSectorAsync("mp1", "worker-1", It.IsAny<DateTime>()))
            .ReturnsAsync((string _, string workerId, DateTime claimedAt) =>
            {
                task.AssignedWorkerId = workerId;
                task.ClaimedAt = claimedAt;
                return task;
            });

        var result = await _sut.GetNextTaskAsync("worker-1", "mp1");

        result.Should().NotBeNull();

        // Claimed but NOT started: the container scan is what makes it InProgress, and
        // that distinction is what keeps the task inside the inactivity sweep's reach
        // until the worker actually begins.
        task.Status.Should().Be(PickTaskStatus.New);
        task.AssignedWorkerId.Should().Be("worker-1");
        task.ClaimedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNextTaskAsync_SweepsExpiredClaimsBeforeClaiming()
    {
        var sequence = new List<string>();

        _pickTaskRepositoryMock
            .Setup(r => r.ReleaseExpiredClaimsAsync("mp1", It.IsAny<DateTime>()))
            .ReturnsAsync(1)
            .Callback(() => sequence.Add("sweep"));
        _pickTaskRepositoryMock
            .Setup(r => r.ClaimNextForSectorAsync("mp1", "worker-1", It.IsAny<DateTime>()))
            .ReturnsAsync((PickTask?)null)
            .Callback(() => sequence.Add("claim"));

        await _sut.GetNextTaskAsync("worker-1", "mp1");

        // Order matters: a task freed by the sweep has to be visible to the claim that
        // follows it in the same transaction, which is the entire trigger mechanism for
        // the inactivity timeout — there is no background job.
        sequence.Should().Equal("sweep", "claim");
    }

    [Fact]
    public async Task GetNextTaskAsync_UsesTheConfiguredClaimTimeoutForTheSweepCutoff()
    {
        var sut = new PickTaskService(
            _unitOfWorkMock.Object,
            new RouteOptimizerService(),
            new UnfulfillableUnitHandler(_unitOfWorkMock.Object, new DefectReplacementPlanner()),
            new ContainerLifecycleService(_unitOfWorkMock.Object),
            new PickTaskSettings { ClaimTimeoutMinutes = 45 });

        DateTime? capturedCutoff = null;
        _pickTaskRepositoryMock
            .Setup(r => r.ReleaseExpiredClaimsAsync("mp1", It.IsAny<DateTime>()))
            .ReturnsAsync(0)
            .Callback((string _, DateTime cutoff) => capturedCutoff = cutoff);
        _pickTaskRepositoryMock
            .Setup(r => r.ClaimNextForSectorAsync("mp1", "worker-1", It.IsAny<DateTime>()))
            .ReturnsAsync((PickTask?)null);

        await sut.GetNextTaskAsync("worker-1", "mp1");

        capturedCutoff.Should().NotBeNull();
        capturedCutoff!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-45), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ReleasePickTaskAsync_SucceedsEvenWhenThereWasNothingToRelease()
    {
        var taskId = Guid.NewGuid();
        _pickTaskRepositoryMock.Setup(r => r.ReleaseClaimAsync(taskId, "worker-1")).ReturnsAsync(false);

        var result = await _sut.ReleasePickTaskAsync(taskId, "worker-1");

        // Not an error: the worker may have started the task, or their claim may already
        // have expired and gone to someone else. Either way the desired end state holds,
        // and this is a best-effort call the client fires while walking away from it.
        result.IsSuccess.Should().BeTrue();
    }
}
