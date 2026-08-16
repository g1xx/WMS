using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Mapping;
using Warehouse.Domain;

namespace Warehouse.Application.Services
{
    public class PickTaskService : IPickTaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRouteOptimizerService _routeOptimizer;
        private readonly IUnfulfillableUnitHandler _unfulfillableUnitHandler;

        public PickTaskService(IUnitOfWork unitOfWork, IRouteOptimizerService routeOptimizer, IUnfulfillableUnitHandler unfulfillableUnitHandler)
        {
            _unitOfWork = unitOfWork;
            _routeOptimizer = routeOptimizer;
            _unfulfillableUnitHandler = unfulfillableUnitHandler;
        }

        public async Task<IEnumerable<PickTaskResponseDto>> GetPickTasksAsync()
        {
            var tasks = await _unitOfWork.PickTasks.GetAllWithDetailsAsync();
            return tasks.Select(MapToDto).ToList();
        }

        public async Task<PickTaskResponseDto?> GetActiveTaskForUserAsync(string userId)
        {
            // Independent of sector on purpose: a worker who gets logged out mid-pick
            // must be able to resume without going through sector selection again.
            var inProgressTask = await _unitOfWork.PickTasks.GetActiveForUserAsync(userId);

            return inProgressTask == null ? null : MapToDto(inProgressTask);
        }

        public async Task<PickTaskResponseDto?> GetNextTaskAsync(string userId, string sector)
        {
            // Strictly segregated: a worker in zone "mp1" only ever sees tasks whose
            // items physically live in "mp1" (PickTask.Sector holds the zone code).
            var nextTask = await _unitOfWork.PickTasks.GetNextForSectorAsync(sector);

            return nextTask == null ? null : MapToDto(nextTask);
        }

        public async Task<Result<string>> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId)
        {
            var task = await _unitOfWork.PickTasks.GetByIdAsync(id);
            if (task == null)
                return Result<string>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (!string.IsNullOrEmpty(task.AssignedWorkerId) && task.AssignedWorkerId != userId)
                return Result<string>.Failure("Error! This task is already being performed by another worker.");

            if (task.Status == PickTaskStatus.Completed)
                return Result<string>.Failure("This task has already been fully picked.");

            // Only ever fetch/suggest containers that are free (New or Available) — an
            // InProgress or Ready container is already committed elsewhere.
            var container = await _unitOfWork.Containers.GetFreeByBarcodeAsync(dto.ContainerBarcode);

            if (container == null)
            {
                // Distinguish "doesn't exist" from "exists but not free" for a clearer message
                var containerExists = await _unitOfWork.Containers.ExistsByBarcodeAsync(dto.ContainerBarcode);

                return containerExists
                    ? Result<string>.Failure(
                        $"Error! Container '{dto.ContainerBarcode}' is not available. Please take an empty one.",
                        ResultErrorType.Conflict)
                    : Result<string>.Failure($"Container with barcode '{dto.ContainerBarcode}' not found.");
            }

            task.Status = PickTaskStatus.InProgress;
            task.AssignedWorkerId = userId;
            task.ContainerId = container.Id;
            container.Status = ContainerStatus.InProgress;
            container.AssignedSector = task.Sector;

            // Move the ORDER itself into the "Picking" status
            var order = await _unitOfWork.Orders.GetByIdAsync(task.OrderId);
            if (order != null && order.Status != OrderStatus.Picking)
            {
                order.Status = OrderStatus.Picking;
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (ConcurrencyConflictException)
            {
                // Another worker (or the dispatch/cancel flow) claimed this task or its
                // container in the moment between our read and this write — the xmin
                // token caught it. Let the caller re-request a task rather than surfacing
                // a raw persistence error.
                return Result<string>.Failure(
                    "This task was just claimed by another worker. Please request a new task.",
                    ResultErrorType.Conflict);
            }

            return Result<string>.Success("Picking successfully started. Container linked, task locked to you.");
        }

        public async Task<Result<string>> PickItemAsync(Guid id, PickItemDto dto, string userId)
        {
            var task = await _unitOfWork.PickTasks.GetByIdWithItemsAndProductLocationAsync(id);

            if (task == null)
                return Result<string>.Failure("Task not found.", ResultErrorType.NotFound);

            if (task.Status != PickTaskStatus.InProgress)
                return Result<string>.Failure("Cannot scan item: task is not active.");

            if (task.AssignedWorkerId != userId)
                return Result<string>.Failure("Access error! The task is being performed by another worker.");

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
                return Result<string>.Failure("Scan error! You are at the wrong location or picked the wrong item.");

            // Units already written off via ReportMissingItemAsync aren't pickable —
            // they were confirmed absent, so they must not count as "still available"
            // headroom for this scan.
            var leftToPick = taskItem.RequiredQuantity - taskItem.PickedQuantity - taskItem.MissingQuantity;
            if (dto.Quantity > leftToPick)
            {
                return Result<string>.Failure($"Over-pick! You only need to pick {leftToPick} more units of this item.");
            }

            var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId);
            if (stock == null)
                return Result<string>.Failure("No stock record found at this location for this product.");

            if (dto.Quantity > stock.PhysicalQuantity)
                return Result<string>.Failure($"Only {stock.PhysicalQuantity} unit(s) are physically on the shelf here.");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. Update the counter on the pick task
                taskItem.PickedQuantity += dto.Quantity;

                // 2. Take the units off this shelf location's stock the instant they're
                //    picked, not at dispatch — a cycle count run while the worker is still
                //    mid-route (routine in a live warehouse) must see what's actually on
                //    the shelf, not stock that's already sitting in the tote.
                stock.PhysicalQuantity -= dto.Quantity;
                stock.ReservedQuantity -= dto.Quantity;

                _unitOfWork.StockTransactions.Add(new StockTransaction
                {
                    ProductId = taskItem.ProductId,
                    LocationId = taskItem.LocationId,
                    QuantityChange = -dto.Quantity,
                    TransactionType = StockTransactionType.Pick,
                    UserId = userId
                });

                // 3. Update the counter on the original order (OrderItem)
                var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(task.OrderId);

                if (order != null)
                {
                    var orderItem = order.Items.FirstOrDefault(oi => oi.ProductId == taskItem.ProductId);
                    if (orderItem != null)
                    {
                        orderItem.PickedQuantity += dto.Quantity;
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });

            return Result<string>.Success($"Successfully picked: {dto.Quantity} units.");
        }

        public async Task<Result<DispatchContainerResultDto>> DispatchContainerAsync(Guid id, DispatchContainerDto dto, string userId)
        {
            var task = await _unitOfWork.PickTasks.GetByIdWithItemsAsync(id);

            if (task == null)
                return Result<DispatchContainerResultDto>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (task.Status != PickTaskStatus.InProgress)
                return Result<DispatchContainerResultDto>.Failure("Task is not in progress.");

            if (task.AssignedWorkerId != userId)
                return Result<DispatchContainerResultDto>.Failure("Task belongs to another worker.");

            var container = await _unitOfWork.Containers.GetByIdAsync(task.ContainerId!.Value);
            if (container == null || container.Barcode != dto.ContainerBarcode)
                return Result<DispatchContainerResultDto>.Failure("Wrong container barcode! Scan the container linked to this task.");

            var station = await _unitOfWork.Locations.GetByBarcodeAsync(dto.ConveyorBarcode);
            if (station == null)
                return Result<DispatchContainerResultDto>.Failure($"Conveyor '{dto.ConveyorBarcode}' was not found.", ResultErrorType.NotFound);

            var newTaskId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                container.LocationId = station.Id;
                // Nothing downstream in this system models a separate "packing station
                // unloads the container" event, so conveyor arrival IS the release point:
                // free the container immediately instead of leaving it stuck in Ready forever.
                container.Status = ContainerStatus.Available;
                container.AssignedSector = null;

                // Units already written off via ReportMissingItemAsync are accounted for,
                // not outstanding — only what's neither picked nor reported missing should
                // roll forward into a new task.
                var leftoverItems = task.Items
                    .Where(i => i.RequiredQuantity > i.PickedQuantity + i.MissingQuantity)
                    .Select(i => new
                    {
                        i.ProductId,
                        i.LocationId,
                        QuantityLeft = i.RequiredQuantity - i.PickedQuantity - i.MissingQuantity
                    })
                    .ToList();

                foreach (var item in task.Items)
                {
                    item.RequiredQuantity = item.PickedQuantity + item.MissingQuantity;
                }

                // Stock for picked units was already taken off the shelf at pick time (see
                // PickItemAsync) — dispatch only finalizes the task/container/order, it no
                // longer touches Stock itself.

                task.Status = PickTaskStatus.Completed;

                // Check whether every line in the order is now resolved — either fully
                // picked, or its outstanding balance permanently written off (ShortedQuantity,
                // set by IUnfulfillableUnitHandler). A resolved order with any written-off
                // line dispatches as ShortShipped, never silently as Packed — the two must
                // stay distinguishable at the order level, not just in task-level history.
                var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(task.OrderId);

                if (order != null)
                {
                    bool isOrderResolved = order.Items.All(i => i.PickedQuantity + i.ShortedQuantity >= i.RequiredQuantity);
                    if (isOrderResolved)
                    {
                        bool isOrderFullyPicked = order.Items.All(i => i.PickedQuantity >= i.RequiredQuantity);
                        order.Status = isOrderFullyPicked ? OrderStatus.Packed : OrderStatus.ShortShipped;
                    }
                }

                Guid? nextTaskId = null;

                if (leftoverItems.Any())
                {
                    var nextTask = new PickTask
                    {
                        OrderId = task.OrderId,
                        Sector = task.Sector,
                        Status = PickTaskStatus.New,
                        AssignedWorkerId = null,
                        ContainerId = null,
                        Items = leftoverItems.Select(l => new PickTaskItem
                        {
                            ProductId = l.ProductId,
                            LocationId = l.LocationId,
                            RequiredQuantity = l.QuantityLeft,
                            PickedQuantity = 0
                        }).ToList()
                    };

                    _unitOfWork.PickTasks.Add(nextTask);
                    await _unitOfWork.SaveChangesAsync();
                    nextTaskId = nextTask.Id;
                }
                else
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                return nextTaskId;
            });

            return Result<DispatchContainerResultDto>.Success(new DispatchContainerResultDto
            {
                Message = "Container successfully verified and sent to the conveyor.",
                NextTaskId = newTaskId
            });
        }

        public async Task<Result<MessageResponseDto>> CancelPickTaskAsync(Guid id, string userId)
        {
            var task = await _unitOfWork.PickTasks.GetByIdWithItemsAsync(id);

            if (task == null)
                return Result<MessageResponseDto>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (task.AssignedWorkerId != userId)
                return Result<MessageResponseDto>.Failure("Access error! The task is being performed by another worker.");

            if (task.Status != PickTaskStatus.InProgress)
                return Result<MessageResponseDto>.Failure("Only a task that is in progress can be cancelled.");

            // Once units have physically been picked into the container, cancelling would strand
            // that stock in an unassigned container — reject instead of silently resetting progress.
            if (task.Items.Any(i => i.PickedQuantity > 0))
                return Result<MessageResponseDto>.Failure("Cannot cancel: some items have already been picked. Report missing items or dispatch the container instead.");

            // Nothing was physically picked into it, so the container is still empty —
            // release it back to the free pool instead of leaving it stuck InProgress.
            if (task.ContainerId.HasValue)
            {
                var container = await _unitOfWork.Containers.GetByIdAsync(task.ContainerId.Value);
                if (container != null)
                {
                    container.Status = ContainerStatus.Available;
                    container.AssignedSector = null;
                }
            }

            // Return the task to its initial state: drop the worker and container
            task.Status = PickTaskStatus.New;
            task.AssignedWorkerId = null;
            task.ContainerId = null;

            await _unitOfWork.SaveChangesAsync();

            return Result<MessageResponseDto>.Success(new MessageResponseDto
            {
                Message = "Pick task cancelled and returned to the queue."
            });
        }

        public async Task<Result<MessageResponseDto>> ReportMissingItemAsync(Guid taskId, ReportMissingItemDto dto, string workerId)
        {
            if (dto.MissingQuantity <= 0)
                return Result<MessageResponseDto>.Failure("Missing quantity must be greater than zero.");

            var task = await _unitOfWork.PickTasks.GetByIdWithItemsAndProductLocationAsync(taskId);

            if (task == null)
                return Result<MessageResponseDto>.Failure("Pick task not found.", ResultErrorType.NotFound);

            // No AssignedWorkerId ownership check here: this action is gated to the
            // Brigadier/Admin role, and the caller is expected to be a supervisor
            // confirming the shortage, not the picker the task is assigned to.

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
                return Result<MessageResponseDto>.Failure("Item not found in this task: wrong location or SKU.");

            var remaining = taskItem.RequiredQuantity - taskItem.PickedQuantity - taskItem.MissingQuantity;
            if (dto.MissingQuantity > remaining)
                return Result<MessageResponseDto>.Failure($"Over-report! Only {remaining} more unit(s) are outstanding on this item.");

            // The units reported missing were never physically there, so they must come off both
            // the physical count and the reservation this task item was holding at this location.
            var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId);

            if (stock == null)
                return Result<MessageResponseDto>.Failure("No stock record found at this location for this product.");

            // The task itself is NOT completed here: the worker still has to physically
            // dispatch the container (see DispatchContainerAsync), which is the only place
            // stock actually moves for this task's other picked items and the container
            // gets released back to the free pool.
            var message = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                taskItem.MissingQuantity += dto.MissingQuantity;

                stock.PhysicalQuantity -= dto.MissingQuantity;
                stock.ReservedQuantity -= dto.MissingQuantity;

                _unitOfWork.StockTransactions.Add(new StockTransaction
                {
                    ProductId = taskItem.ProductId,
                    LocationId = taskItem.LocationId,
                    QuantityChange = -dto.MissingQuantity,
                    TransactionType = StockTransactionType.Missing,
                    UserId = workerId
                });

                // A genuinely missing unit gets the same chance a defective one does: look
                // for a replacement pick in an active picking zone before giving up on it.
                var handlerResult = await _unfulfillableUnitHandler.HandleAsync(
                    task, taskItem.ProductId, taskItem.LocationId, dto.MissingQuantity);

                await _unitOfWork.SaveChangesAsync();

                return BuildUnfulfillableMessage($"{taskItem.MissingQuantity} unit(s) marked missing for this item.", handlerResult);
            });

            return Result<MessageResponseDto>.Success(new MessageResponseDto { Message = message });
        }

        public async Task<Result<ReportDefectResultDto>> ReportDefectAsync(Guid taskId, ReportDefectDto dto, string workerId)
        {
            if (dto.DefectiveQuantity <= 0)
                return Result<ReportDefectResultDto>.Failure("Defective quantity must be greater than zero.");

            var task = await _unitOfWork.PickTasks.GetByIdWithItemsAndProductLocationAsync(taskId);

            if (task == null)
                return Result<ReportDefectResultDto>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (task.Status != PickTaskStatus.InProgress)
                return Result<ReportDefectResultDto>.Failure("Cannot report a defect: task is not active.");

            // No AssignedWorkerId ownership check here: this action is gated to the
            // Brigadier/Admin role, and the caller is expected to be a supervisor
            // confirming the defect, not the picker the task is assigned to.

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
                return Result<ReportDefectResultDto>.Failure("Item not found in this task: wrong location or SKU.");

            var sourceStock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId);

            if (sourceStock == null)
                return Result<ReportDefectResultDto>.Failure("No stock record found at this location for this product.");

            var defectResult = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. Deduct the defective units strictly from PhysicalQuantity. ReservedQuantity
                //    is left untouched here — the reservation this order already holds only moves
                //    once we know where (or whether) it can be replaced below.
                var defectiveQuantity = Math.Min(dto.DefectiveQuantity, sourceStock.PhysicalQuantity);
                sourceStock.PhysicalQuantity -= defectiveQuantity;

                _unitOfWork.StockTransactions.Add(new StockTransaction
                {
                    ProductId = taskItem.ProductId,
                    LocationId = taskItem.LocationId,
                    QuantityChange = -defectiveQuantity,
                    TransactionType = StockTransactionType.Defect,
                    UserId = workerId
                });

                var remainingOnItem = taskItem.RequiredQuantity - taskItem.PickedQuantity;
                var replacementNeeded = Math.Min(defectiveQuantity, remainingOnItem);
                taskItem.RequiredQuantity -= replacementNeeded;

                // The reservation this taskItem held at the source location must shrink by
                // the same amount its requirement just did — otherwise those units stay
                // double-reserved once a replacement location is reserved for them below.
                sourceStock.ReservedQuantity = Math.Max(0, sourceStock.ReservedQuantity - replacementNeeded);

                // 2. Same "find a replacement in an active picking zone, else write off
                //    against the order" logic ReportMissingItemAsync uses, via the shared
                //    handler — what happens once a unit can't be sourced lives in one place.
                var handlerResult = replacementNeeded > 0
                    ? await _unfulfillableUnitHandler.HandleAsync(task, taskItem.ProductId, taskItem.LocationId, replacementNeeded)
                    : new UnfulfillableUnitResult();

                var result = new ReportDefectResultDto
                {
                    DefectiveQuantityDeducted = defectiveQuantity,
                    AppendedToCurrentTaskQuantity = handlerResult.AppendedToCurrentTaskQuantity,
                    NewPickTaskIds = handlerResult.NewPickTaskIds,
                    ShortageQuantity = handlerResult.ShortageQuantity
                };

                await _unitOfWork.SaveChangesAsync();

                result.Message = BuildUnfulfillableMessage($"{result.DefectiveQuantityDeducted} defective unit(s) written off.", handlerResult);
                return result;
            });

            return Result<ReportDefectResultDto>.Success(defectResult);
        }

        private static string BuildUnfulfillableMessage(string leadSentence, UnfulfillableUnitResult result)
        {
            var parts = new List<string> { leadSentence };

            if (result.AppendedToCurrentTaskQuantity > 0)
                parts.Add($"{result.AppendedToCurrentTaskQuantity} unit(s) added to your current task from the same zone.");

            if (result.NewPickTaskIds.Count > 0)
                parts.Add($"{result.NewPickTaskIds.Count} new pick task(s) created in other zones.");

            if (result.ShortageQuantity > 0)
                parts.Add($"{result.ShortageQuantity} unit(s) could not be sourced from active picking zones and were marked pending replenishment.");

            return string.Join(' ', parts);
        }

        private PickTaskResponseDto MapToDto(PickTask task)
        {
            var dto = task.ToDto();
            // Serpentine route: minimizes the worker's walking distance across aisles.
            dto.Items = _routeOptimizer.OptimizeRoute(dto.Items, i => i.LocationBarcode);
            return dto;
        }
    }
}
