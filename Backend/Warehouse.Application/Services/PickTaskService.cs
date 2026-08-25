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
        private readonly IContainerLifecycleService _containerLifecycle;
        private readonly PickTaskSettings _settings;

        public PickTaskService(
            IUnitOfWork unitOfWork,
            IRouteOptimizerService routeOptimizer,
            IUnfulfillableUnitHandler unfulfillableUnitHandler,
            IContainerLifecycleService containerLifecycle,
            PickTaskSettings settings)
        {
            _unitOfWork = unitOfWork;
            _routeOptimizer = routeOptimizer;
            _unfulfillableUnitHandler = unfulfillableUnitHandler;
            _containerLifecycle = containerLifecycle;
            _settings = settings;
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
            // Showing a task IS claiming it. Handing the same task to two workers and then
            // rejecting the second one at the container scan is too late — the second worker
            // has already walked to the racks. The claim moves the rejection to the only
            // moment where it costs nothing: before anyone is told the task exists.
            var claimedTask = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Expired claims are swept first, inside this same transaction, so anything
                // freed here is immediately visible to the claim below. This is the entire
                // trigger mechanism for the inactivity timeout — there is no background job.
                // A stale claim costs nothing while nobody is asking for work in that sector,
                // and this fires exactly when it would start costing something.
                var cutoff = DateTime.UtcNow.AddMinutes(-_settings.ClaimTimeoutMinutes);
                await _unitOfWork.PickTasks.ReleaseExpiredClaimsAsync(sector, cutoff);

                // Strictly segregated: a worker in zone "mp1" only ever sees tasks whose
                // items physically live in "mp1" (PickTask.Sector holds the zone code).
                var task = await _unitOfWork.PickTasks.ClaimNextForSectorAsync(sector, userId, DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync();
                return task;
            });

            return claimedTask == null ? null : MapToDto(claimedTask);
        }

        public async Task<Result<MessageResponseDto>> ReleasePickTaskAsync(Guid id, string userId)
        {
            // Best-effort: fired when the worker leaves picking for the main menu, to put the
            // task back in the queue immediately instead of making the next worker wait out
            // the timeout. If the call is lost — dead battery, network drop, app switch — the
            // inactivity sweep in GetNextTaskAsync is the backstop that always runs, which is
            // why this endpoint never needs to be reliable.
            var released = await _unitOfWork.PickTasks.ReleaseClaimAsync(id, userId);

            // Not an error when nothing was released: the worker may have started the task,
            // or their claim may already have expired and gone to someone else. Either way
            // the desired end state — this worker no longer holds an unstarted task — holds.
            return Result<MessageResponseDto>.Success(new MessageResponseDto
            {
                Message = released ? "Task returned to the queue." : "No claim to release."
            });
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

            // Whether it's actually free is the transition guard's job now, not this
            // query's — a plain lookup here just distinguishes "doesn't exist" from
            // "exists but not claimable," which the guard reports precisely below.
            var container = await _unitOfWork.Containers.GetByBarcodeAsync(dto.ContainerBarcode);
            if (container == null)
                return Result<string>.Failure($"Container with barcode '{dto.ContainerBarcode}' not found.");

            string? claimFailure;
            try
            {
                claimFailure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var transition = await _containerLifecycle.TransitionAsync(container.Id, ContainerTransitions.FreeStatus, ContainerStatus.InProgress);
                    if (!transition.IsSuccess)
                        return transition.Error;

                    task.Status = PickTaskStatus.InProgress;
                    task.AssignedWorkerId = userId;
                    task.ContainerId = container.Id;
                    container.AssignedSector = task.Sector;

                    // Move the ORDER itself into the "Picking" status
                    var order = await _unitOfWork.Orders.GetByIdAsync(task.OrderId);
                    if (order != null && order.Status != OrderStatus.Picking)
                    {
                        order.Status = OrderStatus.Picking;
                    }

                    await _unitOfWork.SaveChangesAsync();
                    return (string?)null;
                });
            }
            catch (ConcurrencyConflictException)
            {
                // Another worker (or the dispatch/cancel flow) claimed this task itself
                // in the moment between our read and this write — the xmin token caught
                // it. The container side of this exact race is now closed by the
                // transition guard above; this remains for the task/order rows' own
                // xmin tokens.
                return Result<string>.Failure(
                    "This task was just claimed by another worker. Please request a new task.",
                    ResultErrorType.Conflict);
            }

            if (claimFailure != null)
                return Result<string>.Failure(claimFailure, ResultErrorType.Conflict);

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

            if (dto.Quantity <= 0)
                return Result<string>.Failure("Quantity must be greater than zero.");

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
                // Clamped like ReportDefectAsync's equivalent decrement — ReservedQuantity
                // can already be out of sync with this task's bookkeeping (e.g. an
                // out-of-band cycle-count correction via InventoryService), so this must
                // not go negative and corrupt Stock.AvailableQuantity.
                stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - dto.Quantity);

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

            // Two distinct ways a task legitimately closes, and exactly one illegitimate one.
            //   anyPicked            -> a real container of goods goes to the conveyor.
            //   fully written off    -> nothing physical to send, but every line's shortfall
            //                          is already recorded as ShortedQuantity, so the task
            //                          must still close for the order to reach ShortShipped.
            //   neither              -> the worker hit "Full container" on an empty tote with
            //                          real work outstanding. That's the bug this guard exists
            //                          for; the client also disables the button (ActiveTaskScreen).
            bool anyPicked = task.Items.Any(i => i.PickedQuantity > 0);
            bool allAccountedFor = task.Items.All(i => i.PickedQuantity + i.MissingQuantity >= i.RequiredQuantity);

            if (!anyPicked && !allAccountedFor)
                return Result<DispatchContainerResultDto>.Failure(
                    "The container is empty — nothing has been picked into it. Cancel the task instead of closing the container.");

            var container = await _unitOfWork.Containers.GetByIdAsync(task.ContainerId!.Value);
            if (container == null || container.Barcode != dto.ContainerBarcode)
                return Result<DispatchContainerResultDto>.Failure("Wrong container barcode! Scan the container linked to this task.");

            var station = await _unitOfWork.Locations.GetByBarcodeAsync(dto.ConveyorBarcode);
            if (station == null)
                return Result<DispatchContainerResultDto>.Failure($"Conveyor '{dto.ConveyorBarcode}' was not found.", ResultErrorType.NotFound);

            // Result<Guid?>, not a bare Guid?, so the container-transition guard's
            // rejection (a real, reachable case — e.g. a duplicate/retried dispatch call
            // racing itself) can travel out of this transaction cleanly instead of being
            // silently discarded or thrown as an unhandled exception.
            var dispatchResult = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Loaded goods go to the conveyor: Ready, still physically full, NOT free.
                // Marking it free here is exactly what let a second worker claim an in-use
                // container; only putaway (once a worker actually empties it) returns a
                // loaded container to the pool.
                //
                // The close-out-empty path is the opposite case: nothing was picked, so
                // nothing reaches the conveyor and the tote is physically empty. It goes
                // straight back to Available — leaving it Ready would strand it waiting on
                // a putaway that has no goods to put away and will never be created.
                var targetStatus = anyPicked ? ContainerStatus.Ready : ContainerTransitions.FreeStatus;

                var transition = await _containerLifecycle.TransitionAsync(container.Id, ContainerStatus.InProgress, targetStatus);
                if (!transition.IsSuccess)
                    return Result<Guid?>.Failure(transition.Error!, transition.ErrorType);

                // Only a container that actually travelled to the conveyor takes the
                // station's location; an empty one never moved, so its recorded position
                // stays whatever it was rather than claiming a conveyor slot it isn't in.
                if (anyPicked)
                    container.LocationId = station.Id;

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

                return Result<Guid?>.Success(nextTaskId);
            });

            if (!dispatchResult.IsSuccess)
                return Result<DispatchContainerResultDto>.Failure(dispatchResult.Error!, dispatchResult.ErrorType);

            return Result<DispatchContainerResultDto>.Success(new DispatchContainerResultDto
            {
                Message = "Container successfully verified and sent to the conveyor.",
                NextTaskId = dispatchResult.Value
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
            //
            // KNOWN GAP (deliberately not fixed here): this guard only looks at PickedQuantity,
            // so a task whose lines were ALL written off as missing has zero picks, passes, and
            // cancels. Nothing in this method touches the Order — DispatchContainerAsync is the
            // only place an order reaches Packed/ShortShipped — so the order stays in Picking
            // with its shortfall already recorded in ShortedQuantity, i.e. stranded. It only
            // recovers if someone re-takes the task from the queue and closes it out via the
            // close-out-empty path in DispatchContainerAsync.
            // Proposed fix: widen this to `i.PickedQuantity + i.MissingQuantity > 0`. A
            // fully-written-off task shouldn't be cancellable at all; it should be closed out,
            // which is what makes the order resolve.
            if (task.Items.Any(i => i.PickedQuantity > 0))
                return Result<MessageResponseDto>.Failure("Cannot cancel: some items have already been picked. Report missing items or dispatch the container instead.");

            var cancelFailure = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Nothing was physically picked into it, so the container is still empty —
                // release it back to the free pool instead of leaving it stuck InProgress.
                if (task.ContainerId.HasValue)
                {
                    var transition = await _containerLifecycle.TransitionAsync(
                        task.ContainerId.Value, ContainerStatus.InProgress, ContainerTransitions.FreeStatus);
                    if (!transition.IsSuccess)
                        return transition.Error;

                    transition.Value!.AssignedSector = null;
                }

                // Return the task to its initial state: drop the worker and container.
                // ClaimedAt goes with AssignedWorkerId — a task back in the queue is
                // unclaimed, and leaving a stale timestamp would make the next worker's
                // fresh claim look already-expired to the sweep.
                task.Status = PickTaskStatus.New;
                task.AssignedWorkerId = null;
                task.ClaimedAt = null;
                task.ContainerId = null;

                await _unitOfWork.SaveChangesAsync();
                return (string?)null;
            });

            if (cancelFailure != null)
                return Result<MessageResponseDto>.Failure(cancelFailure, ResultErrorType.Conflict);

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

            if (task.Status != PickTaskStatus.InProgress)
                return Result<MessageResponseDto>.Failure("Cannot report a shortage: task is not active.");

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
                // Clamped like ReportDefectAsync's equivalent decrement — see PickItemAsync
                // for why this must not go negative.
                stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - dto.MissingQuantity);

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

                // Same "outstanding" formula as PickItemAsync/ReportMissingItemAsync — must
                // also exclude units already written off as missing, or a defect report can
                // overcount what's still outstanding and over-reduce RequiredQuantity below
                // what's already accounted for.
                var remainingOnItem = taskItem.RequiredQuantity - taskItem.PickedQuantity - taskItem.MissingQuantity;
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
