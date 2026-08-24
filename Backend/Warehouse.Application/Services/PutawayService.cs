using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Mapping;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class PutawayService : IPutawayService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRouteOptimizerService _routeOptimizer;
    private readonly IContainerLifecycleService _containerLifecycle;

    public PutawayService(IUnitOfWork unitOfWork, IRouteOptimizerService routeOptimizer, IContainerLifecycleService containerLifecycle)
    {
        _unitOfWork = unitOfWork;
        _routeOptimizer = routeOptimizer;
        _containerLifecycle = containerLifecycle;
    }

    public async Task<Result<PutawayTaskResponseDto>> CreatePutawayTaskAsync(CreatePutawayTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ContainerBarcode))
            return Result<PutawayTaskResponseDto>.Failure("A container barcode is required.");

        if (string.IsNullOrWhiteSpace(dto.Sector))
            return Result<PutawayTaskResponseDto>.Failure("A sector is required.");

        if (dto.Items == null || dto.Items.Count == 0)
            return Result<PutawayTaskResponseDto>.Failure("At least one expected item is required.");

        // Unlike validate/start (which act on containers that already physically
        // exist), this is the seeding entry point — mint a fresh container record
        // if the barcode is new, so a generator tool can hand it an arbitrary ID.
        var container = await _unitOfWork.Containers.GetByBarcodeAsync(dto.ContainerBarcode);
        if (container == null)
        {
            container = new Container
            {
                Barcode = dto.ContainerBarcode,
                Type = ContainerType.Tote,
                // Arrives already loaded from receiving — Ready, not Available (there's
                // nothing to lock against yet at creation, so this is a direct
                // initializer, not a guarded transition; see ContainerLifecycleService).
                Status = ContainerStatus.Ready
            };
            _unitOfWork.Containers.Add(container);
        }

        foreach (var line in dto.Items)
        {
            if (line.ExpectedQuantity <= 0)
                return Result<PutawayTaskResponseDto>.Failure("Expected quantity must be greater than zero for every line.");
        }

        var skus = dto.Items.Select(l => l.ProductSku).Distinct().ToList();
        var productsBySku = await _unitOfWork.Products.GetBySkusAsync(skus);

        var plannedItems = new List<(Product Product, int Quantity)>();

        foreach (var line in dto.Items)
        {
            if (!productsBySku.TryGetValue(line.ProductSku, out var product))
                return Result<PutawayTaskResponseDto>.Failure($"Product with SKU '{line.ProductSku}' was not found.", ResultErrorType.NotFound);

            plannedItems.Add((product, line.ExpectedQuantity));
        }

        // Destinations are chosen by the worker during execution now (see
        // ConfirmItemAsync), so there's no per-item zone to split by anymore —
        // everything lands in one task, routed to the requested sector.
        var task = new PutawayTask
        {
            ContainerId = container.Id,
            Sector = dto.Sector,
            Status = PutawayTaskStatus.New,
            Items = plannedItems.Select(p => new PutawayTaskItem
            {
                ProductId = p.Product.Id,
                ExpectedQuantity = p.Quantity
            }).ToList()
        };

        _unitOfWork.PutawayTasks.Add(task);

        await _unitOfWork.SaveChangesAsync();

        var created = await _unitOfWork.PutawayTasks.GetByIdWithDetailsAsync(task.Id);

        return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(created!));
    }

    public async Task<PutawayTaskResponseDto?> GetActivePutawayTaskForUserAsync(string workerId)
    {
        var task = await _unitOfWork.PutawayTasks.GetActiveForUserAsync(workerId);

        return task == null ? null : await MapToDtoWithSuggestionsAsync(task);
    }

    public async Task<Result<ContainerValidationDto>> ValidateContainerAsync(string containerBarcode, string sector)
    {
        var container = await _unitOfWork.Containers.GetByBarcodeAsync(containerBarcode);
        if (container == null)
            return Result<ContainerValidationDto>.Failure($"Container '{containerBarcode}' was not found.", ResultErrorType.NotFound);

        var pendingTasks = await _unitOfWork.PutawayTasks.GetPendingForContainerAsync(container.Id);

        if (pendingTasks.Count == 0)
            return Result<ContainerValidationDto>.Failure($"No putaway work is pending for container '{containerBarcode}'.", ResultErrorType.NotFound);

        var matchInSector = pendingTasks.FirstOrDefault(t => t.Sector == sector);
        if (matchInSector != null)
        {
            return Result<ContainerValidationDto>.Success(new ContainerValidationDto
            {
                IsValid = true,
                ContainerSector = sector,
                PutawayTaskId = matchInSector.Id,
                Message = "Container validated."
            });
        }

        // Pending work exists, just not in the worker's current sector — a normal,
        // expected outcome the frontend needs to react to, not a system error.
        var otherSector = pendingTasks[0].Sector;
        return Result<ContainerValidationDto>.Success(new ContainerValidationDto
        {
            IsValid = false,
            ContainerSector = otherSector,
            PutawayTaskId = null,
            Message = $"This container is from sector {otherSector}."
        });
    }

    public async Task<Result<PutawayTaskResponseDto>> StartPutawayForContainerAsync(string containerBarcode, string sector, string workerId)
    {
        var container = await _unitOfWork.Containers.GetByBarcodeAsync(containerBarcode);
        if (container == null)
            return Result<PutawayTaskResponseDto>.Failure($"Container '{containerBarcode}' was not found.", ResultErrorType.NotFound);

        // Resume: this worker's own InProgress task for this container/sector
        var task = await _unitOfWork.PutawayTasks.GetInProgressForContainerSectorWorkerAsync(container.Id, sector, workerId);

        if (task == null)
        {
            task = await _unitOfWork.PutawayTasks.GetNewForContainerSectorAsync(container.Id, sector);

            if (task == null)
                return Result<PutawayTaskResponseDto>.Failure(
                    $"No putaway task available for container '{containerBarcode}' in sector {sector}.",
                    ResultErrorType.Conflict);

            var startFailure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var transition = await _containerLifecycle.TransitionAsync(container.Id, ContainerStatus.Ready, ContainerStatus.InProgress);
                if (!transition.IsSuccess)
                    return transition.Error;

                task.Status = PutawayTaskStatus.InProgress;
                task.AssignedWorkerId = workerId;
                container.AssignedSector = sector;
                await _unitOfWork.SaveChangesAsync();
                return (string?)null;
            });

            if (startFailure != null)
                return Result<PutawayTaskResponseDto>.Failure(startFailure, ResultErrorType.Conflict);
        }

        return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(task));
    }

    public async Task<Result<PutawayTaskResponseDto>> ConfirmItemAsync(Guid taskId, ConfirmPutawayItemDto dto, string workerId)
    {
        if (dto.Quantity <= 0)
            return Result<PutawayTaskResponseDto>.Failure("Quantity must be greater than zero.");

        var task = await _unitOfWork.PutawayTasks.GetByIdWithDetailsAsync(taskId);

        if (task == null)
            return Result<PutawayTaskResponseDto>.Failure("Putaway task not found.", ResultErrorType.NotFound);

        if (task.Status != PutawayTaskStatus.InProgress)
            return Result<PutawayTaskResponseDto>.Failure("Cannot scan item: task is not active.");

        if (task.AssignedWorkerId != workerId)
            return Result<PutawayTaskResponseDto>.Failure("Access error! The task is being performed by another worker.");

        var item = task.Items.FirstOrDefault(i => i.Product!.Sku == dto.ProductSku);

        if (item == null)
            return Result<PutawayTaskResponseDto>.Failure("Scan error! Wrong item for this container.");

        var remaining = item.ExpectedQuantity - item.PutAwayQuantity - item.MissingQuantity;
        if (dto.Quantity > remaining)
            return Result<PutawayTaskResponseDto>.Failure($"Over-scan! Only {remaining} more unit(s) are expected here.");

        // The destination is whatever location the worker actually scanned — chosen
        // dynamically during execution, not fixed when the task was created.
        var location = await _unitOfWork.Locations.GetByBarcodeAsync(dto.LocationBarcode);
        if (location == null)
            return Result<PutawayTaskResponseDto>.Failure($"Location '{dto.LocationBarcode}' was not found.", ResultErrorType.NotFound);

        // Nullable string: null means "checks passed, mutation committed"; non-null is
        // a rejection message — either the MaxDistinctSkus check below, or (if this scan
        // completes the task) the container-release transition failing. Using the
        // generic ExecuteInTransactionAsync overload lets either travel straight out of
        // the transaction instead of being stashed in a captured outer variable.
        var transactionFailure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Row lock on the destination first, before reading or writing anything else
            // in this transaction — without it, two concurrent confirms into the same
            // near-full location could both read "room for one more" before either
            // commits. A second concurrent caller targeting the same location blocks
            // here until this transaction commits or rolls back.
            await _unitOfWork.Locations.LockForUpdateAsync(location.Id);

            // Find-or-create respects the unique (ProductId, LocationId) index on Stock —
            // this may be a brand-new pairing if the worker picked a location the
            // product wasn't suggested for.
            var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(item.ProductId, location.Id);

            // A SKU already present here with a non-zero quantity doesn't newly occupy a
            // slot — including one whose Stock row is currently at 0 (previously here,
            // now empty): that's checked exactly like a brand-new SKU would be, not
            // silently exempted.
            var alreadyOccupiesSlot = stock != null && stock.PhysicalQuantity > 0;

            if (!alreadyOccupiesSlot)
            {
                var limit = location.MaxDistinctSkus ?? LocationCapacityDefaults.GetDefaultMaxDistinctSkus(location.Type);
                if (limit.HasValue)
                {
                    var currentDistinctSkuCount = await _unitOfWork.Stocks.CountDistinctProductsWithStockAtLocationAsync(location.Id);
                    if (currentDistinctSkuCount >= limit.Value)
                    {
                        return $"Location '{location.AddressBarcode}' already stocks {currentDistinctSkuCount}/{limit.Value} distinct SKUs " +
                               $"and doesn't currently stock {dto.ProductSku} — choose a different location.";
                    }
                }
            }

            item.PutAwayQuantity += dto.Quantity;

            if (stock == null)
            {
                stock = new Stock
                {
                    ProductId = item.ProductId,
                    LocationId = location.Id,
                    PhysicalQuantity = 0,
                    ReservedQuantity = 0
                };
                _unitOfWork.Stocks.Add(stock);
            }

            stock.PhysicalQuantity += dto.Quantity;

            _unitOfWork.StockTransactions.Add(new StockTransaction
            {
                ProductId = item.ProductId,
                LocationId = location.Id,
                QuantityChange = dto.Quantity,
                TransactionType = StockTransactionType.Putaway,
                UserId = workerId
            });

            if (task.Items.All(i => i.PutAwayQuantity + i.MissingQuantity >= i.ExpectedQuantity))
            {
                task.Status = PutawayTaskStatus.Completed;
                var releaseFailure = await ReleaseContainerIfFullyProcessedAsync(task);
                if (releaseFailure != null)
                    return releaseFailure;
            }

            await _unitOfWork.SaveChangesAsync();

            return (string?)null;
        });

        if (transactionFailure != null)
            return Result<PutawayTaskResponseDto>.Failure(transactionFailure);

        return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(task));
    }

    public async Task<Result<PutawayTaskResponseDto>> ReportMissingAsync(Guid taskId, ReportPutawayMissingDto dto, string workerId)
    {
        if (dto.MissingQuantity <= 0)
            return Result<PutawayTaskResponseDto>.Failure("Missing quantity must be greater than zero.");

        var task = await _unitOfWork.PutawayTasks.GetByIdWithDetailsAsync(taskId);

        if (task == null)
            return Result<PutawayTaskResponseDto>.Failure("Putaway task not found.", ResultErrorType.NotFound);

        if (task.Status != PutawayTaskStatus.InProgress)
            return Result<PutawayTaskResponseDto>.Failure("Cannot report a shortage: task is not active.");

        // No AssignedWorkerId ownership check here: this action is gated to the
        // Brigadier/Admin role, and the caller is expected to be a supervisor
        // confirming the shortage, not the worker the task is assigned to.

        var item = task.Items.FirstOrDefault(i => i.Product!.Sku == dto.ProductSku);

        if (item == null)
            return Result<PutawayTaskResponseDto>.Failure("Item not found in this container: wrong SKU.");

        var remaining = item.ExpectedQuantity - item.PutAwayQuantity - item.MissingQuantity;

        // Uniform over-scan policy: reject rather than silently truncate, matching
        // the strict validation used on the picking side.
        if (dto.MissingQuantity > remaining)
            return Result<PutawayTaskResponseDto>.Failure($"Over-scan! Only {remaining} more unit(s) are expected here.");

        // No stock row is touched here: a putaway shortage means goods that were
        // expected in the container never arrive at a location in the first
        // place, so there is nothing to deduct — only the expectation itself
        // (this item's remaining count) needs to be written off.
        var transactionFailure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            item.MissingQuantity += dto.MissingQuantity;

            if (task.Items.All(i => i.PutAwayQuantity + i.MissingQuantity >= i.ExpectedQuantity))
            {
                task.Status = PutawayTaskStatus.Completed;
                var releaseFailure = await ReleaseContainerIfFullyProcessedAsync(task);
                if (releaseFailure != null)
                    return releaseFailure;
            }

            await _unitOfWork.SaveChangesAsync();
            return (string?)null;
        });

        if (transactionFailure != null)
            return Result<PutawayTaskResponseDto>.Failure(transactionFailure);

        return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(task));
    }

    // A container can hold multiple PutawayTasks (one per destination zone). Only once
    // every one of them is finished is the container actually physically empty and
    // safe to release back into the free pool. Returns a failure message if the
    // release transition was rejected — null on success, including "nothing to
    // release yet, other tasks still pending."
    private async Task<string?> ReleaseContainerIfFullyProcessedAsync(PutawayTask task)
    {
        var otherTasksPending = await _unitOfWork.PutawayTasks.HasOtherActiveTasksForContainerAsync(task.ContainerId, task.Id);

        if (otherTasksPending) return null;

        var transition = await _containerLifecycle.TransitionAsync(task.ContainerId, ContainerStatus.InProgress, ContainerTransitions.FreeStatus);
        if (!transition.IsSuccess)
            return transition.Error;

        transition.Value!.AssignedSector = null;
        return null;
    }

    private async Task<PutawayTaskResponseDto> MapToDtoWithSuggestionsAsync(PutawayTask task)
    {
        var productIds = task.Items.Select(i => i.ProductId).Distinct().ToList();
        var candidatesByProduct = await _unitOfWork.Stocks.GetPutawaySuggestionCandidatesByProductAsync(productIds);

        var allLocationIds = candidatesByProduct.Values
            .SelectMany(candidates => candidates)
            .Select(c => c.LocationId)
            .Distinct()
            .ToList();
        var distinctSkuCountsByLocation = await _unitOfWork.Stocks.GetDistinctSkuCountsByLocationsAsync(allLocationIds);

        var suggestionsByProduct = candidatesByProduct.ToDictionary(
            kvp => kvp.Key,
            kvp => RankSuggestions(kvp.Value, task.Sector, distinctSkuCountsByLocation));

        var dto = task.ToDto(suggestionsByProduct);
        // Serpentine route over each item's top-ranked suggested location: minimizes
        // the worker's walking distance across aisles. Now a meaningful "best
        // consolidation target" rather than an arbitrary DB-ordering artifact, since
        // RankSuggestions below puts same-sector/already-stocked candidates first.
        dto.Items = _routeOptimizer.OptimizeRoute(dto.Items, i => i.SuggestedLocations.FirstOrDefault()?.LocationBarcode);
        return dto;
    }

    // Three groups, in priority order:
    //   1. Same sector, already stocks this SKU (qty > 0)      — top up existing stock.
    //   2. Same sector, held this SKU before, now at 0         — its empty "home slot";
    //      must not be hidden just because quantity is 0.
    //   3. Other sector, currently stocks this SKU (qty > 0)   — informational only, the
    //      worker isn't routed there, just shows the SKU is split across the warehouse.
    //
    // The MaxDistinctSkus exclusion only ever actually drops group-2 candidates: groups 1
    // and 3 both require CurrentQuantity > 0 for THIS product to qualify at all, which by
    // the same rule ConfirmItemAsync uses already exempts them from the limit. A group-2
    // "home slot" that's now full of other SKUs is dropped — ConfirmItemAsync would
    // reject a scan there anyway, so listing it would just be misleading.
    private static List<SuggestedPutawayLocationDto> RankSuggestions(
        List<PutawaySuggestionCandidate> candidates,
        string currentSector,
        IReadOnlyDictionary<Guid, int> distinctSkuCountsByLocation)
    {
        var sameSectorStocked = candidates
            .Where(c => c.ZoneCode == currentSector && c.CurrentQuantity > 0)
            .OrderByDescending(c => c.CurrentQuantity);

        var sameSectorEmptyHomeSlot = candidates
            .Where(c => c.ZoneCode == currentSector && c.CurrentQuantity == 0)
            .Where(c => !IsAtDistinctSkuLimit(c, distinctSkuCountsByLocation))
            .OrderBy(c => c.LocationBarcode);

        var otherSectorStocked = candidates
            .Where(c => c.ZoneCode != currentSector && c.CurrentQuantity > 0)
            .OrderByDescending(c => c.CurrentQuantity);

        return sameSectorStocked
            .Concat(sameSectorEmptyHomeSlot)
            .Concat(otherSectorStocked)
            .Select(c =>
            {
                var limit = c.MaxDistinctSkus ?? LocationCapacityDefaults.GetDefaultMaxDistinctSkus(c.LocationType);
                return new SuggestedPutawayLocationDto
                {
                    LocationBarcode = c.LocationBarcode,
                    CurrentQuantity = c.CurrentQuantity,
                    IsInCurrentSector = c.ZoneCode == currentSector,
                    DistinctSkuCount = distinctSkuCountsByLocation.GetValueOrDefault(c.LocationId),
                    MaxDistinctSkus = limit,
                };
            })
            .ToList();
    }

    private static bool IsAtDistinctSkuLimit(PutawaySuggestionCandidate candidate, IReadOnlyDictionary<Guid, int> distinctSkuCountsByLocation)
    {
        var limit = candidate.MaxDistinctSkus ?? LocationCapacityDefaults.GetDefaultMaxDistinctSkus(candidate.LocationType);
        if (!limit.HasValue) return false;

        return distinctSkuCountsByLocation.GetValueOrDefault(candidate.LocationId) >= limit.Value;
    }
}
