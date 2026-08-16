using FluentAssertions;
using Moq;
using Warehouse.Application.Common;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class InventoryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock;
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _stockTransactionRepositoryMock = new Mock<IStockTransactionRepository>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        _sut = new InventoryService(_unitOfWorkMock.Object);
    }

    private static (Product product, Location location) BuildProductAndLocation()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Widget", Sku = "SKU-1" };
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        return (product, location);
    }

    // ===================== AdjustPhysicalStockAsync =====================

    [Fact]
    public async Task AdjustPhysicalStockAsync_ZeroDelta_ReturnsFailure()
    {
        var result = await _sut.AdjustPhysicalStockAsync(Guid.NewGuid(), "LOC-1", 0, "reason", false, "admin-1");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_NoReason_ReturnsFailure()
    {
        var result = await _sut.AdjustPhysicalStockAsync(Guid.NewGuid(), "LOC-1", 5, "  ", false, "admin-1");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_WouldGoNegative_ReturnsFailureWithoutHittingDb()
    {
        // Arrange: 3 on the shelf, removing 5 would take it negative.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 3, ReservedQuantity = 0 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -5, "cycle count", false, "admin-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        stock.PhysicalQuantity.Should().Be(3);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_NoReservationImpact_SucceedsWithoutRequiringConfirmation()
    {
        // Arrange: 10 physical, only 2 reserved — removing 5 still leaves 5 >= 2 reserved,
        // no reservation is touched.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 10, ReservedQuantity = 2 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -5, "cycle count", false, "admin-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(5);
        stock.ReservedQuantity.Should().Be(2, "nothing reserved was touched");
        result.Value!.ReservedQuantityReduced.Should().Be(0);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_ReservationImpactWithoutConfirmation_ReturnsCleanConflictNotACrash()
    {
        // Arrange: 10 physical, all 10 reserved for an allocated order — a cycle count
        // finding only 4 actually on the shelf would, unguarded, drive PhysicalQuantity
        // below ReservedQuantity and hit the DB check constraint. This must fail cleanly
        // instead, and touch nothing.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 10, ReservedQuantity = 10 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act: -6 would take physical to 4, below the 10 already reserved.
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -6, "cycle count", false, "admin-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("reserved");
        stock.PhysicalQuantity.Should().Be(10, "nothing should be applied until confirmed");
        stock.ReservedQuantity.Should().Be(10);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_ReservationImpactConfirmed_AppliesAndCapsReservation()
    {
        // Arrange: same shortage as above, but this time the caller has already seen the
        // warning and resubmitted with confirmation.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 10, ReservedQuantity = 10 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -6, "cycle count", true, "admin-1");

        // Assert: physical count corrected to ground truth, reservation capped down to
        // match (can never exceed physical), and the impact is reported back explicitly.
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(4);
        stock.ReservedQuantity.Should().Be(4, "reserved can never exceed physical");
        result.Value!.ReservedQuantityReduced.Should().Be(6);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == product.Id &&
            t.LocationId == location.Id &&
            t.QuantityChange == -6 &&
            t.TransactionType == StockTransactionType.ManualAdjustment &&
            t.UserId == "admin-1")), Times.Once);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_PositiveDeltaNeverNeedsConfirmation()
    {
        // Arrange: adding stock can never create a reservation shortfall, regardless of
        // how over-reserved the row already was (a pre-existing, separately-tracked issue
        // this call isn't responsible for) — it should never demand confirmation.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 2, ReservedQuantity = 2 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", 8, "found extra stock", false, "admin-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(10);
        stock.ReservedQuantity.Should().Be(2);
        result.Value!.ReservedQuantityReduced.Should().Be(0);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_ExactlyDrainsToReservedLevel_NoConfirmationNeeded()
    {
        // Arrange: boundary case — new physical quantity lands exactly on ReservedQuantity,
        // not below it. Reserved <= Physical still holds, so this isn't a reservation impact.
        var (product, location) = BuildProductAndLocation();
        var stock = new Stock { ProductId = product.Id, LocationId = location.Id, PhysicalQuantity = 10, ReservedQuantity = 4 };

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync(stock);

        // Act
        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -6, "cycle count", false, "admin-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(4);
        stock.ReservedQuantity.Should().Be(4);
        result.Value!.ReservedQuantityReduced.Should().Be(0);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_ProductNotFound_ReturnsNotFound()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.AdjustPhysicalStockAsync(Guid.NewGuid(), "LOC-1", 5, "reason", false, "admin-1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task AdjustPhysicalStockAsync_NoStockRowAndNegativeDelta_ReturnsFailure()
    {
        var (product, location) = BuildProductAndLocation();

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(product.Id, location.Id)).ReturnsAsync((Stock?)null);

        var result = await _sut.AdjustPhysicalStockAsync(product.Id, "LOC-1", -1, "reason", false, "admin-1");

        result.IsSuccess.Should().BeFalse();
    }
}
