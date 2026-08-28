using FluentAssertions;
using Moq;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class InfoServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IStockRepository> _stockRepositoryMock = new();
    private readonly Mock<ILocationRepository> _locationRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IContainerRepository> _containerRepositoryMock = new();
    private readonly Mock<IPickTaskRepository> _pickTaskRepositoryMock = new();
    private readonly Mock<IPutawayTaskRepository> _putawayTaskRepositoryMock = new();
    private readonly InfoService _sut;

    private readonly Product _product = new() { Id = Guid.NewGuid(), Sku = "SKU-1", Name = "Widget", WeightKg = 2.5m };

    public InfoServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.PickTasks).Returns(_pickTaskRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.PutawayTasks).Returns(_putawayTaskRepositoryMock.Object);

        _productRepositoryMock
            .Setup(r => r.GetBySkusAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, Product> { ["SKU-1"] = _product });

        _pickTaskRepositoryMock.Setup(r => r.GetInProgressForContainerAsync(It.IsAny<Guid>())).ReturnsAsync((PickTask?)null);
        _putawayTaskRepositoryMock.Setup(r => r.GetPendingForContainerAsync(It.IsAny<Guid>())).ReturnsAsync(new List<PutawayTask>());

        _sut = new InfoService(_unitOfWorkMock.Object);
    }

    private static Stock StockAt(Product product, Location location, int physical, int reserved = 0) => new()
    {
        ProductId = product.Id, Product = product, LocationId = location.Id, Location = location,
        PhysicalQuantity = physical, ReservedQuantity = reserved
    };

    [Fact]
    public async Task GetProductInfoAsync_IncludesLocationsSittingAtZero()
    {
        // A zero row is the SKU's home slot, currently empty. Stock rows are never deleted,
        // so this is answerable — but only by a query that declines to filter on quantity.
        var shelf = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000101a", Type = LocationType.Shelf };
        var emptyHome = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000202b", Type = LocationType.Shelf };

        _stockRepositoryMock.Setup(r => r.GetByProductWithLocationAsync(_product.Id)).ReturnsAsync(new List<Stock>
        {
            StockAt(_product, shelf, physical: 10, reserved: 4),
            StockAt(_product, emptyHome, physical: 0),
        });

        var result = await _sut.GetProductInfoAsync("SKU-1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Locations.Should().HaveCount(2);
        result.Value.Locations.Should().Contain(l => l.LocationBarcode == "mp1000202b" && l.PhysicalQuantity == 0);

        var stocked = result.Value.Locations.Single(l => l.LocationBarcode == "mp1000101a");
        stocked.ReservedQuantity.Should().Be(4);
        stocked.AvailableQuantity.Should().Be(6, "available is physical minus what a pick task has reserved");
    }

    [Fact]
    public async Task GetProductInfoAsync_TransitIsExcludedFromTheListButCountedAsCarried()
    {
        var shelf = new Location { Id = Guid.NewGuid(), AddressBarcode = "mp1000101a", Type = LocationType.Shelf };
        var transitOne = new Location { Id = Guid.NewGuid(), AddressBarcode = "TRANSIT-anna", Type = LocationType.Transit };
        var transitTwo = new Location { Id = Guid.NewGuid(), AddressBarcode = "TRANSIT-piotr", Type = LocationType.Transit };

        _stockRepositoryMock.Setup(r => r.GetByProductWithLocationAsync(_product.Id)).ReturnsAsync(new List<Stock>
        {
            StockAt(_product, shelf, physical: 10),
            StockAt(_product, transitOne, physical: 3),
            StockAt(_product, transitTwo, physical: 2),
        });

        var result = await _sut.GetProductInfoAsync("SKU-1");

        // Not somewhere anyone can walk to, so it has no place in a list of addresses...
        result.Value!.Locations.Should().ContainSingle().Which.LocationBarcode.Should().Be("mp1000101a");
        result.Value.Locations.Should().NotContain(l => l.LocationBarcode.StartsWith("TRANSIT-"));

        // ...but the units are real, so they must not silently vanish from the one screen
        // built to answer "where is this SKU".
        result.Value.CarriedByWorkersQuantity.Should().Be(5, "summed across every worker carrying it");
    }

    [Fact]
    public async Task GetProductInfoAsync_UnknownSku_IsNotFound()
    {
        var result = await _sut.GetProductInfoAsync("NOPE");

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(Warehouse.Application.Common.ResultErrorType.NotFound);
    }

    [Fact]
    public async Task GetLocationInfoAsync_ReportsDistinctSkuCountAgainstTheTypeDefaultWhenNoOverride()
    {
        // MaxDistinctSkus is null on the row, so the effective limit is the Shelf default —
        // the same resolution StockPlacementService enforces, so the number shown here is
        // the number a putaway is actually checked against.
        var shelf = new Location
        {
            Id = Guid.NewGuid(), AddressBarcode = "mp1000101a", Type = LocationType.Shelf,
            Sector = "p", WarehouseCode = "m", Floor = 1, MaxDistinctSkus = null
        };

        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(shelf.AddressBarcode)).ReturnsAsync(shelf);
        _stockRepositoryMock.Setup(r => r.GetWithProductAtLocationAsync(shelf.Id)).ReturnsAsync(new List<Stock>
        {
            StockAt(_product, shelf, physical: 7, reserved: 2),
        });
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(shelf.Id)).ReturnsAsync(2);

        var result = await _sut.GetLocationInfoAsync(shelf.AddressBarcode);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DistinctSkuCount.Should().Be(2);
        result.Value.MaxDistinctSkus.Should().Be(LocationCapacityDefaults.GetDefaultMaxDistinctSkus(LocationType.Shelf));
        result.Value.ZoneCode.Should().Be("mp1");
        result.Value.Items.Should().ContainSingle().Which.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public async Task GetLocationInfoAsync_TransitHasNoLimit()
    {
        var transit = new Location
        {
            Id = Guid.NewGuid(), AddressBarcode = "TRANSIT-anna", Type = LocationType.Transit,
            AssignedWorkerId = "worker-1", MaxDistinctSkus = null
        };

        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync(transit.AddressBarcode)).ReturnsAsync(transit);
        _stockRepositoryMock.Setup(r => r.GetWithProductAtLocationAsync(transit.Id)).ReturnsAsync(new List<Stock>());
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(transit.Id)).ReturnsAsync(0);

        var result = await _sut.GetLocationInfoAsync(transit.AddressBarcode);

        // Null means no limit, never zero — a worker's hands are not a storage slot.
        result.Value!.MaxDistinctSkus.Should().BeNull();
    }

    [Fact]
    public async Task GetContainerInfoAsync_ReportsTheHoldingPickTaskAndFlagsContentsUnavailable()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(), Barcode = "CONT-1", Type = ContainerType.Tote,
            Status = ContainerStatus.InProgress, AssignedSector = "mp1",
            Location = new Location { AddressBarcode = "HZA301", Type = LocationType.ConveyorDrop }
        };
        var pickTask = new PickTask
        {
            Id = Guid.NewGuid(), Sector = "mp1", Status = PickTaskStatus.InProgress, ContainerId = container.Id
        };

        _containerRepositoryMock.Setup(r => r.GetByBarcodeWithLocationAsync("CONT-1")).ReturnsAsync(container);
        _pickTaskRepositoryMock.Setup(r => r.GetInProgressForContainerAsync(container.Id)).ReturnsAsync(pickTask);

        var result = await _sut.GetContainerInfoAsync("CONT-1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InProgress");
        result.Value.LocationBarcode.Should().Be("HZA301");
        result.Value.LinkedTask!.Kind.Should().Be("Picking");
        result.Value.LinkedTask.TaskId.Should().Be(pickTask.Id);

        // Contents aren't modelled as Stock (Container.Stocks is always empty), so the
        // client must be able to say "not available yet" rather than render an empty list
        // that a worker would read as "the container is empty".
        result.Value.ContentsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetContainerInfoAsync_FallsBackToAPendingPutawayTask()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(), Barcode = "CONT-2", Type = ContainerType.Tote, Status = ContainerStatus.Ready
        };
        var putawayTask = new PutawayTask
        {
            Id = Guid.NewGuid(), Sector = "mp1", Status = PutawayTaskStatus.New, ContainerId = container.Id
        };

        _containerRepositoryMock.Setup(r => r.GetByBarcodeWithLocationAsync("CONT-2")).ReturnsAsync(container);
        _putawayTaskRepositoryMock
            .Setup(r => r.GetPendingForContainerAsync(container.Id))
            .ReturnsAsync(new List<PutawayTask> { putawayTask });

        var result = await _sut.GetContainerInfoAsync("CONT-2");

        result.Value!.LinkedTask!.Kind.Should().Be("Putaway");
        result.Value.LocationBarcode.Should().BeNull("this container isn't recorded at any location");
    }

    [Fact]
    public async Task GetContainerInfoAsync_UnheldContainerHasNoLinkedTask()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(), Barcode = "CONT-3", Type = ContainerType.Tote, Status = ContainerStatus.Available
        };
        _containerRepositoryMock.Setup(r => r.GetByBarcodeWithLocationAsync("CONT-3")).ReturnsAsync(container);

        var result = await _sut.GetContainerInfoAsync("CONT-3");

        result.Value!.LinkedTask.Should().BeNull();
    }
}
