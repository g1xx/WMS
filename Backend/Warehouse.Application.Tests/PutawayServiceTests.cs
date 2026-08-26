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
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<IStockTransactionRepository> _stockTransactionRepositoryMock;
    private readonly PutawayService _sut;

    public PutawayServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _putawayTaskRepositoryMock = new Mock<IPutawayTaskRepository>();
        _stockRepositoryMock = new Mock<IStockRepository>();
        _containerRepositoryMock = new Mock<IContainerRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _stockTransactionRepositoryMock = new Mock<IStockTransactionRepository>();

        _unitOfWorkMock.Setup(u => u.PutawayTasks).Returns(_putawayTaskRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Stocks).Returns(_stockRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.StockTransactions).Returns(_stockTransactionRepositoryMock.Object);

        // MapToDtoWithSuggestionsAsync always calls these after every mutation — stub them
        // to empty results by default so tests that don't care about suggestions don't
        // need to set them up individually.
        _stockRepositoryMock.Setup(r => r.GetPutawaySuggestionCandidatesByProductAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, List<PutawaySuggestionCandidate>>());
        _stockRepositoryMock.Setup(r => r.GetDistinctSkuCountsByLocationsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // ConfirmItemAsync runs its work inside a transaction; default to transparently
        // running the action, same as the real UnitOfWork does on success, so most tests
        // don't need to set this up individually. Both overloads: ConfirmItemAsync uses
        // the generic one (to carry a MaxDistinctSkus rejection out of the transaction),
        // nothing else in this file currently uses the non-generic one, but it's kept
        // stubbed since Moq's default for an unstubbed Task<T>-returning method silently
        // skips the action entirely rather than throwing — a real gotcha (see the commit
        // that added the MaxDistinctSkus check for how this bit the existing tests here).
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<string?>>>()))
            .Returns<Func<Task<string?>>>(action => action());

        // Defaults so tests that don't care about the MaxDistinctSkus check don't need to
        // set these up individually: no-op lock, and "nothing else stocked here yet" (0
        // is below every LocationCapacityDefaults value, including Shelf's default of 3 —
        // the Type every test Location implicitly gets, since LocationType.Shelf is the
        // enum's default value).
        _locationRepositoryMock
            .Setup(r => r.LockForUpdateAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _stockRepositoryMock
            .Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        // Real implementations, not mocks: route ordering is pure logic with no
        // dependencies of its own, and ContainerLifecycleService's only dependency is
        // the already-mocked IUnitOfWork — using the real thing here exercises its
        // actual lock/re-read/validate logic instead of assuming it works.
        // Real StockPlacementService, not a mock: the MaxDistinctSkus tests in this file
        // exist to validate that capacity rule, and it now lives there. Stubbing it would
        // leave those tests asserting against a fake and the real rule uncovered.
        _sut = new PutawayService(
            _unitOfWorkMock.Object,
            new RouteOptimizerService(),
            new ContainerLifecycleService(_unitOfWorkMock.Object),
            new StockPlacementService(_unitOfWorkMock.Object));
    }

    // Wires the container mocks ContainerLifecycleService.TransitionAsync needs for a
    // scenario that completes the task and releases the container: LockForUpdateAsync
    // must report the container's current (pre-transition) status, and GetByIdAsync
    // must return the SAME tracked instance so the transition's mutation is visible to
    // the test's own assertions afterward.
    private void StubContainerRelease(PutawayTask task)
    {
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(task.ContainerId)).ReturnsAsync(task.Container!.Status);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(task.ContainerId)).ReturnsAsync(task.Container);
    }

    // Single-item InProgress task: expected 10, put away 0, missing 0, SKU matching
    // the dtos used below. Container is attached so ReleaseContainerIfFullyProcessedAsync
    // can resolve it via task.Container without an extra repository round-trip. The item
    // does not carry its own location — the worker supplies one at scan time.
    private static PutawayTask BuildTaskWithOneItem(
        string assignedWorkerId = "worker-1",
        int expectedQuantity = 10,
        int putAwayQuantity = 0,
        int missingQuantity = 0,
        ContainerStatus containerStatus = ContainerStatus.InProgress)
    {
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
        // Arrange: 4 of 10 expected still to go, scanning 4 more (not the last unit),
        // put away at a location the worker chose (LOC-1).
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        var item = task.Items.First();
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        var stock = new Stock { ProductId = item.ProductId, LocationId = location.Id, PhysicalQuantity = 20, ReservedQuantity = 5 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, location.Id)).ReturnsAsync(stock);
        StubContainerRelease(task); // this scan completes the item (6 + 4 = 10), so it releases the container

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 4 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.PutAwayQuantity.Should().Be(10);
        stock.PhysicalQuantity.Should().Be(24);

        _stockTransactionRepositoryMock.Verify(r => r.Add(It.Is<StockTransaction>(t =>
            t.ProductId == item.ProductId &&
            t.LocationId == location.Id &&
            t.QuantityChange == 4 &&
            t.TransactionType == StockTransactionType.Putaway)), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmItemAsync_LocationNotFound_ReturnsFailure()
    {
        // Arrange: the worker scanned a barcode that doesn't resolve to any known location.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 0);

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("NOWHERE")).ReturnsAsync((Location?)null);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "NOWHERE", ProductSku = "SKU-1", Quantity = 1 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("was not found");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmItemAsync_LastItemInContainer_ReleasesContainer()
    {
        // Arrange: the only item on the only task for this container, and this scan
        // fills its full expected quantity — nothing else is holding the container.
        var task = BuildTaskWithOneItem(expectedQuantity: 5, putAwayQuantity: 0);
        var item = task.Items.First();
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };
        var stock = new Stock { ProductId = item.ProductId, LocationId = location.Id, PhysicalQuantity = 0, ReservedQuantity = 0 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, location.Id)).ReturnsAsync(stock);
        _putawayTaskRepositoryMock.Setup(r => r.HasOtherActiveTasksForContainerAsync(task.ContainerId, task.Id)).ReturnsAsync(false);
        StubContainerRelease(task);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 5 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PutawayTaskStatus.Completed);
        task.Container!.Status.Should().Be(ContainerStatus.Available);
        task.Container!.AssignedSector.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmItemAsync_TransactionThrows_PropagatesExceptionInsteadOfSwallowingIt()
    {
        // Arrange: a transaction failure (e.g. a concurrency conflict) must propagate
        // to the caller rather than being swallowed — the global exception handler is
        // the only thing responsible for turning it into a response.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1" };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<string?>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 4 };

        // Act
        Func<Task> act = () => _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===================== ConfirmItemAsync: MaxDistinctSkus =====================

    [Fact]
    public async Task ConfirmItemAsync_AtLimitWithNewSku_ReturnsFailure()
    {
        // Arrange: location already stocks 2 distinct SKUs against a limit of 2, and the
        // one being confirmed here isn't one of them.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 0);
        var item = task.Items.First();
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1", MaxDistinctSkus = 2 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, location.Id)).ReturnsAsync((Stock?)null);
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(location.Id)).ReturnsAsync(2);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 1 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2/2");
        result.Error.Should().Contain("SKU-1");
        item.PutAwayQuantity.Should().Be(0);

        _stockRepositoryMock.Verify(r => r.Add(It.IsAny<Stock>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmItemAsync_AtLimitButSkuAlreadyStockedHere_Succeeds()
    {
        // Arrange: location is at its limit of 2, but the SKU being confirmed is already
        // one of the 2 it stocks (non-zero quantity) — adding more of it isn't a new
        // distinct SKU, so the limit shouldn't even be consulted.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 0);
        var item = task.Items.First();
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1", MaxDistinctSkus = 2 };
        var stock = new Stock { ProductId = item.ProductId, LocationId = location.Id, PhysicalQuantity = 5, ReservedQuantity = 0 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, location.Id)).ReturnsAsync(stock);
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(location.Id)).ReturnsAsync(2);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 3 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        stock.PhysicalQuantity.Should().Be(8);

        // Proves the check was skipped entirely, not just coincidentally satisfied.
        _stockRepositoryMock.Verify(r => r.CountDistinctProductsWithStockAtLocationAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmItemAsync_SkuHadZeroQuantityRowHere_CheckedAsNewNotExempt()
    {
        // Arrange: this exact SKU was stocked here before and is now at 0 — per spec that
        // doesn't occupy a slot, so it must be checked exactly like a brand-new SKU would
        // be, not automatically exempted just because a Stock row already exists.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 0);
        var item = task.Items.First();
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1", MaxDistinctSkus = 2 };
        var stock = new Stock { ProductId = item.ProductId, LocationId = location.Id, PhysicalQuantity = 0, ReservedQuantity = 0 };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(item.ProductId, location.Id)).ReturnsAsync(stock);
        _stockRepositoryMock.Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(location.Id)).ReturnsAsync(2);

        var dto = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-1", Quantity = 3 };

        // Act
        var result = await _sut.ConfirmItemAsync(task.Id, dto, "worker-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2/2");
        stock.PhysicalQuantity.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmItemAsync_ConcurrentConfirmsIntoSameNearFullLocation_ExactlyOneSucceeds()
    {
        // Arrange: two different products, one task with a line for each, one shared
        // destination location at MaxDistinctSkus=2 already stocking exactly 1 distinct
        // SKU (neither of the two below) — one free slot, contested by both at once.
        //
        // This simulates what a real "SELECT ... FOR UPDATE" transaction would guarantee:
        // LockForUpdateAsync blocks the second caller until the first's whole transaction
        // (not just the lock call) finishes, and only a successful transaction increments
        // the committed count the next contender reads. A fixed/unsynchronized stub for
        // CountDistinctProductsWithStockAtLocationAsync would let both callers read "1"
        // and both pass — exactly the bug this test exists to catch.
        var productA = new Product { Id = Guid.NewGuid(), Sku = "SKU-A" };
        var productB = new Product { Id = Guid.NewGuid(), Sku = "SKU-B" };
        var location = new Location { Id = Guid.NewGuid(), AddressBarcode = "LOC-1", MaxDistinctSkus = 2 };
        var container = new Container { Id = Guid.NewGuid(), Status = ContainerStatus.InProgress, AssignedSector = "mp1" };

        var task = new PutawayTask
        {
            Id = Guid.NewGuid(),
            ContainerId = container.Id,
            Container = container,
            Status = PutawayTaskStatus.InProgress,
            AssignedWorkerId = "worker-1",
            Items = new List<PutawayTaskItem>
            {
                new() { ProductId = productA.Id, Product = productA, ExpectedQuantity = 10 },
                new() { ProductId = productB.Id, Product = productB, ExpectedQuantity = 10 },
            }
        };

        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);
        _locationRepositoryMock.Setup(r => r.GetByBarcodeAsync("LOC-1")).ReturnsAsync(location);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(productA.Id, location.Id)).ReturnsAsync((Stock?)null);
        _stockRepositoryMock.Setup(r => r.GetByProductAndLocationAsync(productB.Id, location.Id)).ReturnsAsync((Stock?)null);

        var committedDistinctCount = 1;
        _stockRepositoryMock
            .Setup(r => r.CountDistinctProductsWithStockAtLocationAsync(location.Id))
            .Returns(() => Task.FromResult(committedDistinctCount));

        // Real SemaphoreSlim standing in for the row lock: acquired inside
        // LockForUpdateAsync, released only when the surrounding "transaction" below
        // finishes — not when the lock call itself returns.
        var locationLock = new SemaphoreSlim(1, 1);
        _locationRepositoryMock
            .Setup(r => r.LockForUpdateAsync(location.Id))
            .Returns(() => locationLock.WaitAsync());

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<string?>>>()))
            .Returns<Func<Task<string?>>>(async action =>
            {
                try
                {
                    var outcome = await action();
                    // Mirrors a real commit: only a successful transaction actually adds a
                    // new distinct SKU, so only that case should affect what the next
                    // contender (waiting on the lock) reads.
                    if (outcome == null) committedDistinctCount++;
                    return outcome;
                }
                finally
                {
                    locationLock.Release();
                }
            });

        var dtoA = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-A", Quantity = 1 };
        var dtoB = new ConfirmPutawayItemDto { LocationBarcode = "LOC-1", ProductSku = "SKU-B", Quantity = 1 };

        // Act: genuinely concurrent — both start before either can have completed.
        var taskA = _sut.ConfirmItemAsync(task.Id, dtoA, "worker-1");
        var taskB = _sut.ConfirmItemAsync(task.Id, dtoB, "worker-1");
        var results = await Task.WhenAll(taskA, taskB);

        // Assert: exactly one succeeded, the other lost the race and was rejected on the
        // capacity check — never both, which is what a missing or misordered lock would
        // allow.
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => !r.IsSuccess).Should().Be(1);
        results.Single(r => !r.IsSuccess).Error.Should().Contain("distinct SKUs");
    }

    // ===================== ReportMissingAsync =====================

    [Fact]
    public async Task ReportMissingAsync_MoreThanRemaining_ReturnsOverScanFailure()
    {
        // Arrange: only 4 units remain unaccounted for, reporting 5 as missing over-scans.
        var task = BuildTaskWithOneItem(expectedQuantity: 10, putAwayQuantity: 6);
        _putawayTaskRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(task.Id)).ReturnsAsync(task);

        var dto = new ReportPutawayMissingDto { ProductSku = "SKU-1", MissingQuantity = 5 };

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
        StubContainerRelease(task); // this report completes the item (6 + 4 = 10), so it releases the container

        var dto = new ReportPutawayMissingDto { ProductSku = "SKU-1", MissingQuantity = 4 };

        // Act
        var result = await _sut.ReportMissingAsync(task.Id, dto, "supervisor-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.MissingQuantity.Should().Be(4);

        // A putaway shortage means goods never physically arrived — there is nothing to
        // deduct from inbound stock, unlike a picking shortage.
        _locationRepositoryMock.Verify(r => r.GetByBarcodeAsync(It.IsAny<string>()), Times.Never);
        _stockRepositoryMock.Verify(r => r.GetByProductAndLocationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _stockRepositoryMock.Verify(r => r.Add(It.IsAny<Stock>()), Times.Never);
        _stockTransactionRepositoryMock.Verify(r => r.Add(It.IsAny<StockTransaction>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---- Creating a task stages the container ---------------------------------------
    // A container with putaway work planned against it is about to be loaded at receiving,
    // so it must stop being claimable for picking the moment the task exists.

    private CreatePutawayTaskDto StubCreateScenario(Container? existingContainer)
    {
        var productsMock = new Mock<IProductRepository>();
        _unitOfWorkMock.Setup(u => u.Products).Returns(productsMock.Object);

        var product = new Product { Id = Guid.NewGuid(), Sku = "SKU-1" };
        productsMock
            .Setup(r => r.GetBySkusAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, Product> { ["SKU-1"] = product });

        _containerRepositoryMock.Setup(r => r.GetByBarcodeAsync("CONT-1")).ReturnsAsync(existingContainer);

        if (existingContainer != null)
        {
            _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(existingContainer.Id)).ReturnsAsync(existingContainer.Status);
            _containerRepositoryMock.Setup(r => r.GetByIdAsync(existingContainer.Id)).ReturnsAsync(existingContainer);
        }

        _putawayTaskRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new PutawayTask { Id = id, Sector = "mp1", Items = new List<PutawayTaskItem>() });

        return new CreatePutawayTaskDto
        {
            ContainerBarcode = "CONT-1",
            Sector = "mp1",
            Items = new List<CreatePutawayTaskItemDto> { new() { ProductSku = "SKU-1", ExpectedQuantity = 5 } }
        };
    }

    [Fact]
    public async Task CreatePutawayTaskAsync_ExistingFreeContainer_IsStagedToReady()
    {
        var container = new Container { Id = Guid.NewGuid(), Barcode = "CONT-1", Status = ContainerTransitions.FreeStatus };
        var dto = StubCreateScenario(container);

        var result = await _sut.CreatePutawayTaskAsync(dto);

        result.IsSuccess.Should().BeTrue($"creating the task should succeed: {result.Error}");

        // The bug this fixes: leaving it Available lets a picker claim a container that
        // receiving is about to load, out from under the putaway.
        container.Status.Should().Be(ContainerStatus.Ready);

        // Proves the status above came from the guarded transition and not from some
        // incidental default — the row lock is what makes the claim safe against a picker
        // claiming the same container concurrently.
        _containerRepositoryMock.Verify(r => r.LockForUpdateAsync(container.Id), Times.Once);
    }

    [Fact]
    public async Task CreatePutawayTaskAsync_ContainerAlreadyStaged_IsLeftAlone()
    {
        // Several putaway tasks per container is normal (see GetPendingForContainerAsync);
        // the second one must not try to re-run a transition that already happened, which
        // the lifecycle guard would reject as a conflict.
        var container = new Container { Id = Guid.NewGuid(), Barcode = "CONT-1", Status = ContainerStatus.Ready };
        var dto = StubCreateScenario(container);

        var result = await _sut.CreatePutawayTaskAsync(dto);

        result.IsSuccess.Should().BeTrue($"a second task for a staged container is legitimate: {result.Error}");
        container.Status.Should().Be(ContainerStatus.Ready);
        _containerRepositoryMock.Verify(r => r.LockForUpdateAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreatePutawayTaskAsync_NewContainer_IsCreatedReadyWithoutATransition()
    {
        var dto = StubCreateScenario(existingContainer: null);

        Container? added = null;
        _containerRepositoryMock.Setup(r => r.Add(It.IsAny<Container>())).Callback((Container c) => added = c);

        var result = await _sut.CreatePutawayTaskAsync(dto);

        result.IsSuccess.Should().BeTrue($"creating the task should succeed: {result.Error}");
        added.Should().NotBeNull();

        // Arrives already loaded from receiving. There's no committed row to lock against
        // yet, so this is a direct initializer rather than a guarded transition.
        added!.Status.Should().Be(ContainerStatus.Ready);
        _containerRepositoryMock.Verify(r => r.LockForUpdateAsync(It.IsAny<Guid>()), Times.Never);
    }
}
