using FluentAssertions;
using Moq;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class OrderAllocationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IPickTaskRepository> _pickTaskRepositoryMock;
    private readonly OrderAllocationService _sut;

    public OrderAllocationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _pickTaskRepositoryMock = new Mock<IPickTaskRepository>();

        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.PickTasks).Returns(_pickTaskRepositoryMock.Object);

        _sut = new OrderAllocationService(_unitOfWorkMock.Object);
    }

    // WarehouseCode + Sector + Floor drive the computed ZoneCode ("mp1") the service
    // groups planned picks by when deciding how many PickTasks to create.
    private static Location BuildLocation(string warehouseCode = "m", string sector = "p", int floor = 1) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseCode = warehouseCode,
        Sector = sector,
        Floor = floor,
        AddressBarcode = $"{warehouseCode}{sector}{floor}0100101a"
    };

    private static Stock BuildStock(Guid productId, Location location, int physicalQuantity, int reservedQuantity = 0) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = productId,
        LocationId = location.Id,
        Location = location,
        PhysicalQuantity = physicalQuantity,
        ReservedQuantity = reservedQuantity
    };

    private static Order BuildOrderWithItems(params OrderItem[] items) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = "ORD-TEST-1",
        Status = OrderStatus.New,
        Items = items.ToList()
    };

    private static OrderItem BuildOrderItem(Guid productId, int requiredQuantity) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = productId,
        RequiredQuantity = requiredQuantity,
        PickedQuantity = 0
    };

    [Fact]
    public async Task AllocateOrderAsync_SingleLocationHasEnoughStock_AllocatesFullyAndCreatesPickTask()
    {
        // Arrange: one line needing 5 units, one location holding 15 available (20 physical, 5 reserved).
        var productId = Guid.NewGuid();
        var location = BuildLocation();
        var stock = BuildStock(productId, location, physicalQuantity: 20, reservedQuantity: 5);
        var orderItem = BuildOrderItem(productId, requiredQuantity: 5);
        var order = BuildOrderWithItems(orderItem);

        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _stockRepositoryMock.Setup(r => r.GetAvailableForProductsAsync(It.Is<List<Guid>>(ids => ids.Contains(productId))))
            .ReturnsAsync(new List<Stock> { stock });

        // Act
        var (isAllocated, message) = await _sut.AllocateOrderAsync(order.Id);

        // Assert
        isAllocated.Should().BeTrue();
        message.Should().BeNull();

        stock.ReservedQuantity.Should().Be(10); // 5 (already reserved) + 5 (this allocation)
        order.Status.Should().Be(OrderStatus.Picking);

        _pickTaskRepositoryMock.Verify(r => r.Add(It.Is<PickTask>(pt =>
            pt.OrderId == order.Id &&
            pt.Sector == location.ZoneCode &&
            pt.Items.Count == 1 &&
            pt.Items.First().ProductId == productId &&
            pt.Items.First().LocationId == location.Id &&
            pt.Items.First().RequiredQuantity == 5)), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AllocateOrderAsync_NoSingleLocationHasEnough_SplitsAcrossLocations()
    {
        // Arrange: 15 required; location A has 10 available, location B has 5 available —
        // neither alone covers it, but together they exactly do.
        var productId = Guid.NewGuid();
        var locationA = BuildLocation();
        var locationB = BuildLocation();
        var stockA = BuildStock(productId, locationA, physicalQuantity: 10, reservedQuantity: 0);
        var stockB = BuildStock(productId, locationB, physicalQuantity: 5, reservedQuantity: 0);
        var orderItem = BuildOrderItem(productId, requiredQuantity: 15);
        var order = BuildOrderWithItems(orderItem);

        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _stockRepositoryMock.Setup(r => r.GetAvailableForProductsAsync(It.Is<List<Guid>>(ids => ids.Contains(productId))))
            .ReturnsAsync(new List<Stock> { stockA, stockB });

        // Act
        var (isAllocated, message) = await _sut.AllocateOrderAsync(order.Id);

        // Assert
        isAllocated.Should().BeTrue();
        message.Should().BeNull();

        // Neither location's available quantity is exceeded, and together they sum to the requirement.
        stockA.ReservedQuantity.Should().Be(10);
        stockB.ReservedQuantity.Should().Be(5);
        order.Status.Should().Be(OrderStatus.Picking);

        _pickTaskRepositoryMock.Verify(r => r.Add(It.Is<PickTask>(pt =>
            pt.Items.Count == 2 &&
            pt.Items.Sum(i => i.RequiredQuantity) == 15 &&
            pt.Items.Any(i => i.LocationId == locationA.Id && i.RequiredQuantity == 10) &&
            pt.Items.Any(i => i.LocationId == locationB.Id && i.RequiredQuantity == 5))), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AllocateOrderAsync_InsufficientStockForALine_ParksWholeOrderWithoutCreatingAnyPickTask()
    {
        // Arrange: the order has two lines. The first (processed first, per Items order)
        // needs 20 units but only 5 are available anywhere — a real shortage. The second
        // line has ample stock on its own. Current implementation aborts the whole dry run
        // the moment ANY line comes up short, so the second (perfectly allocatable) line
        // must also end up with no PickTask — this is an all-or-nothing-per-order allocator,
        // not a partial one.
        var shortProductId = Guid.NewGuid();
        var plentifulProductId = Guid.NewGuid();

        var shortLocation = BuildLocation();
        var plentifulLocation = BuildLocation();

        var shortStock = BuildStock(shortProductId, shortLocation, physicalQuantity: 5, reservedQuantity: 0);
        var plentifulStock = BuildStock(plentifulProductId, plentifulLocation, physicalQuantity: 100, reservedQuantity: 0);

        var shortItem = BuildOrderItem(shortProductId, requiredQuantity: 20);
        var plentifulItem = BuildOrderItem(plentifulProductId, requiredQuantity: 5);
        var order = BuildOrderWithItems(shortItem, plentifulItem);

        _orderRepositoryMock.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _stockRepositoryMock.Setup(r => r.GetAvailableForProductsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<Stock> { shortStock, plentifulStock });

        // Act
        var (isAllocated, message) = await _sut.AllocateOrderAsync(order.Id);

        // Assert
        isAllocated.Should().BeFalse();
        message.Should().Contain("Shortage detected").And.Contain("15"); // 20 required - 5 available

        order.Status.Should().Be(OrderStatus.AwaitingReplenishment);

        // Nothing was committed for either line — not even the fully-coverable one.
        shortStock.ReservedQuantity.Should().Be(0);
        plentifulStock.ReservedQuantity.Should().Be(0);
        _pickTaskRepositoryMock.Verify(r => r.Add(It.IsAny<PickTask>()), Times.Never);

        // The parked status still gets persisted.
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
