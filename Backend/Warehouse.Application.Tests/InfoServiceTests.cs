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

    // ---- Container contents ---------------------------------------------------------
    // Derived state, reconstructed from task lines because Stock carries no ContainerId.
    // Each rule is pinned separately, because each carries different confidence and the
    // failure mode of getting one wrong is reporting a falsehood as inventory.

    private static Container ContainerWith(ContainerStatus status) => new()
    {
        Id = Guid.NewGuid(), Barcode = "HSOD00015", Type = ContainerType.Tote, Status = status
    };

    private void StubContainer(Container container)
    {
        _containerRepositoryMock.Setup(r => r.GetByBarcodeWithLocationAsync(container.Barcode)).ReturnsAsync(container);
        _pickTaskRepositoryMock.Setup(r => r.GetInProgressForContainerAsync(container.Id)).ReturnsAsync((PickTask?)null);
        _pickTaskRepositoryMock.Setup(r => r.GetMostRecentCompletedForContainerAsync(container.Id)).ReturnsAsync((PickTask?)null);
        _putawayTaskRepositoryMock.Setup(r => r.GetPendingWithItemsForContainerAsync(container.Id)).ReturnsAsync(new List<PutawayTask>());
    }

    private PickTask CompletedPick(Guid containerId, int picked) => new()
    {
        Id = Guid.NewGuid(), ContainerId = containerId, Sector = "mp1", Status = PickTaskStatus.Completed,
        Items = new List<PickTaskItem>
        {
            new() { ProductId = _product.Id, Product = _product, RequiredQuantity = picked, PickedQuantity = picked }
        }
    };

    [Fact]
    public async Task ContainerContents_AvailableMeansEmpty_FromStatusNotFromLines()
    {
        // Emptiness is stored: ReleaseContainerIfFullyProcessedAsync sets Available exactly
        // when all putaway work finished. Deriving it from task lines instead would also
        // read a brand-new container's ABSENCE of lines as emptiness, a different claim.
        var container = ContainerWith(ContainerStatus.Available);
        StubContainer(container);

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        result.Value!.ContentSections.Should().ContainSingle().Which.Kind.Should().Be("Empty");
    }

    [Fact]
    public async Task ContainerContents_AvailableWinsOverLeftoverDispatchHistory()
    {
        // Released to the free pool means a putaway emptied it, so whatever a previous
        // dispatch put in is gone.
        var container = ContainerWith(ContainerStatus.Available);
        StubContainer(container);
        _pickTaskRepositoryMock
            .Setup(r => r.GetMostRecentCompletedForContainerAsync(container.Id))
            .ReturnsAsync(CompletedPick(container.Id, 6));

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        result.Value!.ContentSections.Should().ContainSingle().Which.Kind.Should().Be("Empty");
    }

    [Fact]
    public async Task ContainerContents_UnknownWhenNothingWasEverRecorded()
    {
        // Ready with no task history at all. Absence of data is not emptiness — saying
        // "empty" here would be inventing a fact.
        var container = ContainerWith(ContainerStatus.Ready);
        StubContainer(container);

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        result.Value!.ContentSections.Should().ContainSingle().Which.Kind.Should().Be("Unknown");
    }

    [Fact]
    public async Task ContainerContents_DispatchedContainerReportsPickedLinesAsHistory()
    {
        // The HSOD00015 case: Ready, held by no task, its pick task completed at dispatch.
        var container = ContainerWith(ContainerStatus.Ready);
        StubContainer(container);
        var dispatched = CompletedPick(container.Id, 6);
        _pickTaskRepositoryMock
            .Setup(r => r.GetMostRecentCompletedForContainerAsync(container.Id))
            .ReturnsAsync(dispatched);

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        var section = result.Value!.ContentSections.Should().ContainSingle().Subject;
        section.Kind.Should().Be("AsDispatched");
        section.SourceTaskId.Should().Be(dispatched.Id);
        section.Lines.Should().ContainSingle().Which.Quantity.Should().Be(6);

        // Load-bearing: the client must render this differently from a live line, because
        // it is a statement about the past that nothing ever invalidates.
        section.IsHistorical.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerContents_PickedThenPartlyPutAway_ReportsTwoFactsAndNeverSubtracts()
    {
        // The case that cannot be answered with one number: PutawayTaskItem.ExpectedQuantity
        // is supplied by whoever created the task, not derived from what was picked, so the
        // two figures are not guaranteed to describe the same physical units.
        var container = ContainerWith(ContainerStatus.InProgress);
        StubContainer(container);
        _pickTaskRepositoryMock
            .Setup(r => r.GetMostRecentCompletedForContainerAsync(container.Id))
            .ReturnsAsync(CompletedPick(container.Id, 6));

        var other = new Product { Id = Guid.NewGuid(), Sku = "SKU-2", Name = "Gadget" };
        _putawayTaskRepositoryMock
            .Setup(r => r.GetPendingWithItemsForContainerAsync(container.Id))
            .ReturnsAsync(new List<PutawayTask>
            {
                new()
                {
                    Id = Guid.NewGuid(), ContainerId = container.Id, Sector = "mp1",
                    Status = PutawayTaskStatus.InProgress,
                    Items = new List<PutawayTaskItem>
                    {
                        new() { ProductId = other.Id, Product = other, ExpectedQuantity = 10, PutAwayQuantity = 4 }
                    }
                }
            });

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        result.Value!.ContentSections.Select(s => s.Kind)
            .Should().BeEquivalentTo(new[] { "ToBePutAway", "AsDispatched" });

        // Each stands on its own; neither is reconciled against the other.
        result.Value.ContentSections.Single(s => s.Kind == "AsDispatched")
            .Lines.Should().ContainSingle().Which.Quantity.Should().Be(6);
        result.Value.ContentSections.Single(s => s.Kind == "ToBePutAway")
            .Lines.Should().ContainSingle().Which.Quantity.Should().Be(6, "10 expected minus 4 already put away");
    }

    [Fact]
    public async Task ContainerContents_ActivePickTaskSupersedesDispatchHistory()
    {
        var container = ContainerWith(ContainerStatus.InProgress);
        StubContainer(container);
        var active = new PickTask
        {
            Id = Guid.NewGuid(), ContainerId = container.Id, Sector = "mp1", Status = PickTaskStatus.InProgress,
            Items = new List<PickTaskItem>
            {
                new() { ProductId = _product.Id, Product = _product, RequiredQuantity = 5, PickedQuantity = 2 }
            }
        };
        _pickTaskRepositoryMock.Setup(r => r.GetInProgressForContainerAsync(container.Id)).ReturnsAsync(active);
        _pickTaskRepositoryMock
            .Setup(r => r.GetMostRecentCompletedForContainerAsync(container.Id))
            .ReturnsAsync(CompletedPick(container.Id, 99));

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        // A live claim beats a historical one — stale lines from a previous task must not
        // sit beside what is being picked into it right now.
        result.Value!.ContentSections.Should().ContainSingle().Which.Kind.Should().Be("BeingPickedInto");
        result.Value.ContentSections.Single().Lines.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task ContainerInfo_ListsEveryPendingPutawayTaskNotJustTheFirst()
    {
        // A container legitimately has one putaway task per zone. Reporting a single "the"
        // task picks arbitrarily among them and presents that choice as fact.
        var container = ContainerWith(ContainerStatus.Ready);
        StubContainer(container);
        _putawayTaskRepositoryMock
            .Setup(r => r.GetPendingWithItemsForContainerAsync(container.Id))
            .ReturnsAsync(new List<PutawayTask>
            {
                new() { Id = Guid.NewGuid(), ContainerId = container.Id, Sector = "mp1", Status = PutawayTaskStatus.New, Items = new List<PutawayTaskItem>() },
                new() { Id = Guid.NewGuid(), ContainerId = container.Id, Sector = "mr1", Status = PutawayTaskStatus.New, Items = new List<PutawayTaskItem>() },
            });

        var result = await _sut.GetContainerInfoAsync(container.Barcode);

        result.Value!.LinkedTasks.Should().HaveCount(2);
        result.Value.LinkedTasks.Select(t => t.Sector).Should().BeEquivalentTo(new[] { "mp1", "mr1" });
    }
}
