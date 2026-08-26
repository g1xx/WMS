using FluentAssertions;
using Moq;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class RelocationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStockRepository> _stockRepositoryMock = new();
    private readonly Mock<ILocationRepository> _locationRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock = new();
    private readonly RelocationService _sut;

    private readonly Product _product = new() { Id = Guid.NewGuid(), Sku = "SKU-1", Name = "Widget" };
    private readonly Location _source = new()
    {
        Id = Guid.NewGuid(), AddressBarcode = "mp1000101a", Type = LocationType.Shelf
    };
    private readonly Location _transit = new()
    {
        Id = Guid.NewGuid(), AddressBarcode = "TRANSIT-worker-1", Type = LocationType.Transit,
        AssignedWorkerId = "worker-1", MaxDistinctSkus = null
    };

    public RelocationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        // Both legs run inside the generic string? overload; an unstubbed Task<T> method
        // silently skips the action rather than throwing (same gotcha as the other suites).
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<string?>>>()))
            .Returns<Func<Task<string?>>>(action => action());

        _locationRepositoryMock
            .Setup(r => r.GetOrCreateTransitForWorkerAsync("worker-1", It.IsAny<string>()))
            .ReturnsAsync(_transit);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(_source.AddressBarcode)).ReturnsAsync(_source);
        _locationRepositoryMock.Setup(r => r.GetByIdAsync(_transit.Id)).ReturnsAsync(_transit);
        _locationRepositoryMock.Setup(r => r.GetByIdAsync(_source.Id)).ReturnsAsync(_source);

        _productRepositoryMock
            .Setup(r => r.GetBySkusAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, Product> { ["SKU-1"] = _product });

        // Nothing carried unless a test says otherwise.
        _stockRepositoryMock
            .Setup(r => r.GetWithProductAtLocationAsync(_transit.Id))
            .ReturnsAsync(new List<Stock>());

        _sut = new RelocationService(_unitOfWorkMock.Object, new StockPlacementService(_unitOfWorkMock.Object));
    }

    private void StubSourceStock(int physical, int reserved)
    {
        var stock = new Stock
        {
            ProductId = _product.Id, Product = _product, LocationId = _source.Id,
            PhysicalQuantity = physical, ReservedQuantity = reserved
        };
        _stockRepositoryMock.Setup(r => r.LockForUpdateAsync(_product.Id, _source.Id)).ReturnsAsync((physical, reserved));
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, _source.Id)).ReturnsAsync(stock);
    }

    private RelocationTakeDto Take(int quantity) => new()
    {
        SourceLocationBarcode = _source.AddressBarcode, ProductSku = "SKU-1", Quantity = quantity
    };

    [Fact]
    public async Task TakeAsync_ReservedUnitsAreNotRelocatable()
    {
        // 10 on the shelf, 4 already reserved for a pick task. Only 6 may move — relocating
        // a reserved unit sends the picker who reserved it to an empty slot.
        StubSourceStock(physical: 10, reserved: 4);

        var result = await _sut.TakeAsync("worker-1", "worker-1", Take(7));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("6").And.Contain("reserved");
    }

    [Fact]
    public async Task TakeAsync_UpToAvailableQuantity_Succeeds()
    {
        StubSourceStock(physical: 10, reserved: 4);

        var result = await _sut.TakeAsync("worker-1", "worker-1", Take(6));

        result.IsSuccess.Should().BeTrue($"6 of 10 are unreserved: {result.Error}");
    }

    [Fact]
    public async Task TakeAsync_EverythingReserved_ExplainsWhyRatherThanSayingEmpty()
    {
        StubSourceStock(physical: 5, reserved: 5);

        var result = await _sut.TakeAsync("worker-1", "worker-1", Take(1));

        result.IsSuccess.Should().BeFalse();
        // The shelf is visibly full, so "no stock here" would look like a bug to the worker.
        result.Error.Should().Contain("reserved");
    }

    [Fact]
    public async Task TakeAsync_DecidesOnTheLockedQuantityNotAStaleTrackedRow()
    {
        // The lock reports the committed truth (2 left); the tracked entity is stale at 10,
        // as it would be if something earlier in the request had already read it. The
        // decision must follow the lock, or a worker takes stock that isn't there.
        var stale = new Stock
        {
            ProductId = _product.Id, Product = _product, LocationId = _source.Id,
            PhysicalQuantity = 10, ReservedQuantity = 0
        };
        _stockRepositoryMock.Setup(r => r.LockForUpdateAsync(_product.Id, _source.Id)).ReturnsAsync((2, 0));
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, _source.Id)).ReturnsAsync(stale);

        var result = await _sut.TakeAsync("worker-1", "worker-1", Take(6));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2");
    }

    [Fact]
    public async Task TakeAsync_WritesBothLegsToTheAuditTrail()
    {
        StubSourceStock(physical: 10, reserved: 0);

        await _sut.TakeAsync("worker-1", "worker-1", Take(4));

        // Out of the shelf...
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.LocationId == _source.Id && t.QuantityChange == -4
            && t.TransactionType == StockTransactionType.Relocation)), Times.Once);
        // ...and into the worker's hands. Two rows, so the movement is reconstructable.
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.LocationId == _transit.Id && t.QuantityChange == 4
            && t.TransactionType == StockTransactionType.Relocation)), Times.Once);
    }

    [Fact]
    public async Task TakeAsync_FromATransitLocation_IsRejected()
    {
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("TRANSIT-someone-else")).ReturnsAsync(
            new Location { Id = Guid.NewGuid(), AddressBarcode = "TRANSIT-someone-else", Type = LocationType.Transit });

        var result = await _sut.TakeAsync("worker-1", "worker-1", new RelocationTakeDto
        {
            SourceLocationBarcode = "TRANSIT-someone-else", ProductSku = "SKU-1", Quantity = 1
        });

        result.IsSuccess.Should().BeFalse("stock in someone's hands is not on a shelf to take from");
    }

    [Fact]
    public async Task PutAwayAsync_PartialQuantity_LeavesTheRestCarried()
    {
        // Splitting one carried SKU across several targets: place 3 of 10, keep 7.
        var target = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000202b", Type = LocationType.Shelf, MaxDistinctSkus = 3 };
        var carried = new Stock
        {
            ProductId = _product.Id, Product = _product, LocationId = _transit.Id,
            PhysicalQuantity = 10, ReservedQuantity = 0
        };

        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(target.AddressBarcode)).ReturnsAsync(target);
        _locationRepositoryMock.Setup(r => r.GetByIdAsync(target.Id)).ReturnsAsync(target);
        _stockRepositoryMock.Setup(r => r.LockForUpdateAsync(_product.Id, _transit.Id)).ReturnsAsync((10, 0));
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, _transit.Id)).ReturnsAsync(carried);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, target.Id)).ReturnsAsync((Stock?)null);
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(target.Id)).ReturnsAsync(0);

        var result = await _sut.PutAwayAsync("worker-1", "worker-1", new RelocationPutawayDto
        {
            TargetLocationBarcode = target.AddressBarcode, ProductSku = "SKU-1", Quantity = 3
        });

        result.IsSuccess.Should().BeTrue($"a partial placement is normal: {result.Error}");
        carried.PhysicalQuantity.Should().Be(7, "the remainder stays in the worker's hands for the next target");
    }

    [Fact]
    public async Task PutAwayAsync_TargetAtSkuLimit_IsRejectedAndNothingLeavesTheWorkersHands()
    {
        // MaxDistinctSkus applies normally when putting away into a real location.
        var target = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000303c", Type = LocationType.Shelf, MaxDistinctSkus = 2 };
        var carried = new Stock
        {
            ProductId = _product.Id, Product = _product, LocationId = _transit.Id,
            PhysicalQuantity = 5, ReservedQuantity = 0
        };

        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(target.AddressBarcode)).ReturnsAsync(target);
        _locationRepositoryMock.Setup(r => r.GetByIdAsync(target.Id)).ReturnsAsync(target);
        _stockRepositoryMock.Setup(r => r.LockForUpdateAsync(_product.Id, _transit.Id)).ReturnsAsync((5, 0));
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, _transit.Id)).ReturnsAsync(carried);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(_product.Id, target.Id)).ReturnsAsync((Stock?)null);
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(target.Id)).ReturnsAsync(2);

        var result = await _sut.PutAwayAsync("worker-1", "worker-1", new RelocationPutawayDto
        {
            TargetLocationBarcode = target.AddressBarcode, ProductSku = "SKU-1", Quantity = 5
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("SKU-1", "the refusal has to name what the worker is holding");
        carried.PhysicalQuantity.Should().Be(5, "a refused placement must not take units off the worker");
    }

    [Fact]
    public async Task PutAwayAsync_MoreThanCarried_IsRejected()
    {
        var target = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000404d", Type = LocationType.Shelf };
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(target.AddressBarcode)).ReturnsAsync(target);
        _stockRepositoryMock.Setup(r => r.LockForUpdateAsync(_product.Id, _transit.Id)).ReturnsAsync((2, 0));

        var result = await _sut.PutAwayAsync("worker-1", "worker-1", new RelocationPutawayDto
        {
            TargetLocationBarcode = target.AddressBarcode, ProductSku = "SKU-1", Quantity = 5
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2");
    }

    [Fact]
    public async Task GetStateAsync_CanExitOnlyWhenNothingIsCarried()
    {
        var empty = await _sut.GetStateAsync("worker-1", "worker-1");
        empty.CanExit.Should().BeTrue();

        _stockRepositoryMock
            .Setup(r => r.GetWithProductAtLocationAsync(_transit.Id))
            .ReturnsAsync(new List<Stock>
            {
                new() { ProductId = _product.Id, Product = _product, LocationId = _transit.Id, PhysicalQuantity = 3 }
            });

        var holding = await _sut.GetStateAsync("worker-1", "worker-1");
        holding.CanExit.Should().BeFalse("a worker must not walk away holding stock");
        holding.CarriedItems.Should().ContainSingle().Which.AvailableQuantity.Should().Be(3);
    }
}
