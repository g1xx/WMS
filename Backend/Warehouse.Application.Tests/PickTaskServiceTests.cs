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
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock;
    private readonly PickTaskService _sut;

    public PickTaskServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pickTaskRepositoryMock = new Mock<IPickTaskRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _containerRepositoryMock = new Mock<IContainerRepository>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _stockTransactionRepositoryMock = new Mock<IStockTransactionRepository>();

        _unitOfWorkMock.Setup(u => u.PickTasks).Returns(_pickTaskRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        _sut = new PickTaskService(_unitOfWorkMock.Object);
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
    public async Task ReportMissingItemAsync_AllItemsFullyAccountedFor_CompletesTask()
    {
        // Arrange: one item, required 10, already picked 7 — reporting the last 3 as
        // missing means 7 + 3 == 10, so every line is now fully resolved.
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
        task.Status.Should().Be(PickTaskStatus.Completed);
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
    public async Task StartPickTaskAsync_ConcurrentClaimDetected_ReturnsConflictFailure()
    {
        // Arrange: another worker's request (or a cancel/dispatch) commits first and the
        // xmin token trips on this save — UnitOfWork translates EF's DbUpdateConcurrencyException
        // into ConcurrencyConflictException, which the service must turn into a Result, not
        // let bubble up as an unhandled exception.
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
            Status = ContainerStatus.New
        };

        _pickTaskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
        _containerRepositoryMock.Setup(r => r.GetFreeByBarcodeAsync("CONT-1")).ReturnsAsync(container);
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
}
