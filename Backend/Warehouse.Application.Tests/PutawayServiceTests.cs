using FluentAssertions;
using Moq;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class PutawayServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPutawayTaskRepository> _putawayTaskRepositoryMock;
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IContainerRepository> _containerRepositoryMock;
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock;
    private readonly PutawayService _sut;

    public PutawayServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _putawayTaskRepositoryMock = new Mock<IPutawayTaskRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _containerRepositoryMock = new Mock<IContainerRepository>();
        _stockTransactionRepositoryMock = new Mock<IStockTransactionRepository>();

        _unitOfWorkMock.Setup(u => u.PutawayTasks).Returns(_putawayTaskRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        _sut = new PutawayService(_unitOfWorkMock.Object);
    }

    // Single-item InProgress task: expected 10, put away 0, missing 0, located/skus
    // matching the dtos used below. Container is attached so ReleaseContainerIfFullyProcessedAsync
    // can resolve it via task.Container without an extra repository round-trip.
    private static PutawayTask BuildTaskWithOneItem(
        string assignedWorkerId = "worker-1",
        int expectedQuantity = 10,
        int putAwayQuantity = 0,
        int missingQuantity = 0,
        ContainerStatus containerStatus = ContainerStatus.InProgress)
    {
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        var product = new Product { Id = Guid.NewGuid(), Sku = "SKU-1" };
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Status = containerStatus,
            AssignedSector = "mp1"
        };

        return new PutawayTask
        {
            Id = Guid.NewGuid(),
            ContainerId = container.Id,
            Container = container,
            Status = PutawayTaskStatus.InProgress,
            AssignedWorkerId = assignedWorkerId,
            Items = new List<PutawayTaskItem>
            {
                new()
                {
                    ProductId = product.Id,
                    Product = product,
                    DestinationLocationId = location.Id,
                    DestinationLocation = location,
                    ExpectedQuantity = expectedQuantity,
                    PutAwayQuantity = putAwayQuantity,
                    MissingQuantity = missingQuantity
                }
            }
        };
    }

    // ===================== ConfirmItemAsync =====================

    [Fact]
    public async Task ConfirmItemAsync_ValidScan_IncreasesStockAndLogsTransaction()
    {
        // Arrange: 4 of 10 expected still to go, scanning 4 more (not the last unit).
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        var item = task.Items.First();
        var stock = new Stock { ProductId = item.ProductId, LocationId = item.DestinationLocationId, PhysicalQuantity = 20, ReservedQuantity = 5 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, item.DestinationLocationId)).ReturnsAsync(stock);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 4 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.PutAwayQuantity.Should().Be(10);
        stock.PhysicalQuantity.Should().Be(24);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == item.ProductId &&
            t.LocationId == item.DestinationLocationId &&
            t.QuantityChange == 4 &&
            t.TransactionType == StockTransactionType.Putaway)), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmItemAsync_LastItemInContainer_ReleasesContainer()
    {
        // Arrange: the only item on the only task for this container, and this scan
        // fills its full expected quantity — nothing else is holding the container.
        var task = BuildTaskWithOneItem(expectedQuantity: 5, putAwayQuantity: 0);
        var item = task.Items.First();
        var stock = new Stock { ProductId = item.ProductId, LocationId = item.DestinationLocationId, PhysicalQuantity = 0, ReservedQuantity = 0 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, item.DestinationLocationId)).ReturnsAsync(stock);
        _putawayTaskRepositoryMock.Setup(r => r.HasOtherActiveTasksForContainerAsync(task.ContainerId, task.Id)).ReturnsAsync(false);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 5 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PutawayTaskStatus.Completed);
        task.Container!.Status.Should().Be(ContainerStatus.Available);
        task.Container!.AssignedSector.Should().BeNull();
    }

    // ===================== ReportMissingAsync =====================

    [Fact]
    public async Task ReportMissingAsync_MoreThanRemaining_ReturnsOverScanFailure()
    {
        // Arrange: only 4 units remain unaccounted for, reporting 5 as missing over-scans.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);

        var dto = new ReportPutawayMissingDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", MissingQuantity = 5 };

        // Act
        var result = await _sut.ReportMissingAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Over-scan");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReportMissingAsync_ValidReport_UpdatesMissingQuantityWithoutTouchingStock()
    {
        // Arrange
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        var item = task.Items.First();

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);

        var dto = new ReportPutawayMissingDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", MissingQuantity = 4 };

        // Act
        var result = await _sut.ReportMissingAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.MissingQuantity.Should().Be(4);

        // A putaway shortage means goods never physically arrived — there is nothing to
        // deduct from inbound stock, unlike a picking shortage.
        _stockRepositoryMock.Verify(r => r.GetByProductAndLocationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _stockRepositoryMock.Verify(r => r.Add(It.IsAny<Stock>()), Times.Never);
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.IsAny<StockTransaction>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
