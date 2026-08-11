using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class PutawayService : IPutawayService
{
    private readonly IUnitOfWork _unitOfWork;

    public PutawayService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
                Status = ContainerStatus.New
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

            task.Status = PutawayTaskStatus.InProgress;
            task.AssignedWorkerId = workerId;
            container.Status = ContainerStatus.InProgress;
            container.AssignedSector = sector;
            await _unitOfWork.SaveChangesAsync();
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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            item.PutAwayQuantity += dto.Quantity;

            // Find-or-create respects the unique (ProductId, LocationId) index on Stock —
            // this may be a brand-new pairing if the worker picked a location the
            // product wasn't suggested for.
            var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(item.ProductId, location.Id);

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
                await ReleaseContainerIfFullyProcessedAsync(task);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(task));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
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
        item.MissingQuantity += dto.MissingQuantity;

        if (task.Items.All(i => i.PutAwayQuantity + i.MissingQuantity >= i.ExpectedQuantity))
        {
            task.Status = PutawayTaskStatus.Completed;
            await ReleaseContainerIfFullyProcessedAsync(task);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result<PutawayTaskResponseDto>.Success(await MapToDtoWithSuggestionsAsync(task));
    }

    // A container can hold multiple PutawayTasks (one per destination zone). Only once
    // every one of them is finished is the container actually physically empty and
    // safe to release back into the free pool.
    private async Task ReleaseContainerIfFullyProcessedAsync(PutawayTask task)
    {
        var otherTasksPending = await _unitOfWork.PutawayTasks.HasOtherActiveTasksForContainerAsync(task.ContainerId, task.Id);

        if (otherTasksPending) return;

        var container = task.Container ?? await _unitOfWork.Containers.GetByIdAsync(task.ContainerId);
        if (container != null)
        {
            container.Status = ContainerStatus.Available;
            container.AssignedSector = null;
        }
    }

    private async Task<PutawayTaskResponseDto> MapToDtoWithSuggestionsAsync(PutawayTask task)
    {
        var productIds = task.Items.Select(i => i.ProductId).Distinct().ToList();
        var suggestionsByProduct = await _unitOfWork.Stocks.GetLocationBarcodesByProductAsync(productIds);

        return new PutawayTaskResponseDto
        {
            Id = task.Id,
            ContainerBarcode = task.Container?.Barcode ?? string.Empty,
            Sector = task.Sector,
            Status = task.Status.ToString(),
            Items = task.Items.Select(i => new PutawayTaskItemResponseDto
            {
                Id = i.Id,
                ProductName = i.Product!.Name,
                ProductSku = i.Product.Sku,
                ExpectedQuantity = i.ExpectedQuantity,
                PutAwayQuantity = i.PutAwayQuantity,
                MissingQuantity = i.MissingQuantity,
                SuggestedLocationBarcodes = suggestionsByProduct.TryGetValue(i.ProductId, out var barcodes)
                    ? barcodes
                    : new List<string>()
            }).ToList()
        };
    }
}
