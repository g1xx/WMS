using FluentAssertions;
using Moq;
using Warehouse.Application.Common;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class ContainerLifecycleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContainerRepository> _containerRepositoryMock;
    private readonly ContainerLifecycleService _sut;

    public ContainerLifecycleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _containerRepositoryMock = new Mock<IContainerRepository>();
        _unitOfWorkMock.Setup(u => u.Containers).Returns(_containerRepositoryMock.Object);

        _sut = new ContainerLifecycleService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task TransitionAsync_ValidTransition_MutatesAndReturnsTheContainer()
    {
        var container = new Container { Id = Guid.NewGuid(), Status = ContainerStatus.Available };
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(container.Status);
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);

        var result = await _sut.TransitionAsync(container.Id, ContainerStatus.Available, ContainerStatus.InProgress);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ContainerStatus.InProgress);
        container.Status.Should().Be(ContainerStatus.InProgress);
    }

    [Fact]
    public async Task TransitionAsync_CurrentStatusDoesNotMatchFrom_ReturnsConflict()
    {
        // The fresh lock-read reports InProgress even though `from` asks for Available —
        // someone else already claimed it since the caller's own (unlocked) read.
        var container = new Container { Id = Guid.NewGuid(), Status = ContainerStatus.InProgress };
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(container.Id)).ReturnsAsync(ContainerStatus.InProgress);

        var result = await _sut.TransitionAsync(container.Id, ContainerStatus.Available, ContainerStatus.InProgress);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("currently InProgress");

        // Never even attempted to fetch/mutate the tracked entity for a rejected transition.
        _containerRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TransitionAsync_ContainerNotFound_ReturnsNotFound()
    {
        var containerId = Guid.NewGuid();
        _containerRepositoryMock.Setup(r => r.LockForUpdateAsync(containerId)).ReturnsAsync((ContainerStatus?)null);

        var result = await _sut.TransitionAsync(containerId, ContainerStatus.Available, ContainerStatus.InProgress);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task TransitionAsync_PairNotInAllowList_Throws()
    {
        // Ready -> Available frees a container that is still physically LOADED — the exact
        // bug the Ready status exists to prevent, and the reason it can only be left via
        // InProgress (a worker emptying it). Emptiness is never assumed, only observed.
        //
        // This used to assert on Available -> Ready, which became legal when putaway task
        // creation started staging a free container at receiving.
        var containerId = Guid.NewGuid();

        Func<Task> act = () => _sut.TransitionAsync(containerId, ContainerStatus.Ready, ContainerTransitions.FreeStatus);

        await act.Should().ThrowAsync<InvalidOperationException>();

        // A code bug, not a business outcome — must reject before ever touching the DB.
        _containerRepositoryMock.Verify(r => r.LockForUpdateAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TransitionAsync_ConcurrentClaimsOnSameContainer_ExactlyOneSucceeds()
    {
        // Two callers racing to claim the same Available container. A real
        // "SELECT ... FOR UPDATE" would block the second until the first's transaction
        // commits, then its own fresh read would see the post-commit status. A
        // SemaphoreSlim simulates that: acquired inside the lock call, released only
        // once each full claim (mutation included) finishes — not when the lock call
        // itself returns — mirroring the MaxDistinctSkus concurrency test.
        var container = new Container { Id = Guid.NewGuid(), Status = ContainerStatus.Available };
        _containerRepositoryMock.Setup(r => r.GetByIdAsync(container.Id)).ReturnsAsync(container);

        var containerLock = new SemaphoreSlim(1, 1);
        _containerRepositoryMock
            .Setup(r => r.LockForUpdateAsync(container.Id))
            .Returns(async () =>
            {
                await containerLock.WaitAsync();
                // Read AFTER acquiring, not before — this is what makes the second
                // contender see the first's already-committed mutation.
                return container.Status;
            });

        async Task<Result<Container>> ClaimAndRelease()
        {
            try
            {
                return await _sut.TransitionAsync(container.Id, ContainerStatus.Available, ContainerStatus.InProgress);
            }
            finally
            {
                containerLock.Release();
            }
        }

        // Act: genuinely concurrent — both start before either can have completed.
        var results = await Task.WhenAll(ClaimAndRelease(), ClaimAndRelease());

        // Assert: exactly one succeeded, the other lost the race — never both, which is
        // what a missing or misordered lock would allow.
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => !r.IsSuccess).Should().Be(1);
        results.Single(r => !r.IsSuccess).ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
