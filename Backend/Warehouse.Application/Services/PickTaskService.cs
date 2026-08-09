using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services
{
    public class PickTaskService : IPickTaskService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PickTaskService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            if (taskItem.PickedQuantity + dto.Quantity > taskItem.RequiredQuantity)
            {
                var leftToPick = taskItem.RequiredQuantity - taskItem.PickedQuantity;
                return Result<string>.Failure($"Over-pick! You only need to pick {leftToPick} more units of this item.");
            }

            // 1. Update the counter on the pick task
            taskItem.PickedQuantity += dto.Quantity;

            // 2. Update the counter on the original order (OrderItem)
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                container.LocationId = station.Id;
                // Nothing downstream in this system models a separate "packing station
                // unloads the container" event, so conveyor arrival IS the release point:
                // free the container immediately instead of leaving it stuck in Ready forever.
                container.Status = ContainerStatus.Available;
                container.AssignedSector = null;

                var leftoverItems = task.Items
                    .Where(i => i.RequiredQuantity > i.PickedQuantity)
                    .Select(i => new
                    {
                        i.ProductId,
                        i.LocationId,
                        QuantityLeft = i.RequiredQuantity - i.PickedQuantity
                    })
                    .ToList();

                foreach (var item in task.Items)
                {
                    item.RequiredQuantity = item.PickedQuantity;
                }

                // Clear the reservation and remove the physically picked units from
                // stock at their source location — the container now owns them.
                foreach (var item in task.Items.Where(i => i.PickedQuantity > 0))
                {
                    var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(item.ProductId, item.LocationId);

                    if (stock != null)
                    {
                        stock.PhysicalQuantity -= item.PickedQuantity;
                        stock.ReservedQuantity -= item.PickedQuantity;

                        _unitOfWork.StockTransactions.Add(new StockTransaction
                        {
                            ProductId = item.ProductId,
                            LocationId = item.LocationId,
                            QuantityChange = -item.PickedQuantity,
                            TransactionType = StockTransactionType.Pick,
                            UserId = userId
                        });
                    }
                }

                task.Status = PickTaskStatus.Completed;

                // Check whether the whole order has now been picked
                var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(task.OrderId);

                if (order != null)
                {
                    // If no unpicked lines remain anywhere in the order
                    bool isOrderFullyPicked = order.Items.All(i => i.PickedQuantity >= i.RequiredQuantity);
                    if (isOrderFullyPicked)
                    {
                        order.Status = OrderStatus.Packed;
                    }
                }

                Guid? newTaskId = null;

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
                    newTaskId = nextTask.Id;
                }
                else
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                return Result<DispatchContainerResultDto>.Success(new DispatchContainerResultDto
                {
                    Message = "Container successfully verified and sent to the conveyor.",
                    NextTaskId = newTaskId
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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

            taskItem.MissingQuantity += dto.MissingQuantity;

            // The units reported missing were never physically there, so they must come off both
            // the physical count and the reservation this task item was holding at this location.
            var stock = await _unitOfWork.Stocks.GetByProductAndLocationAsync(taskItem.ProductId, taskItem.LocationId);

            if (stock == null)
                return Result<MessageResponseDto>.Failure("No stock record found at this location for this product.");

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

            // Only close the task out once every line is fully accounted for (picked or missing).
            if (task.Items.All(i => i.PickedQuantity + i.MissingQuantity == i.RequiredQuantity))
            {
                task.Status = PickTaskStatus.Completed;
            }

            await _unitOfWork.SaveChangesAsync();

            return Result<MessageResponseDto>.Success(new MessageResponseDto
            {
                Message = $"Missing item reported. {taskItem.MissingQuantity} unit(s) marked missing for this item."
            });
        }

        // Bulk/high-rack storage sector (e.g. the "w" in "mw1") — never a valid
        // source for direct picking or defect-replacement reallocation.
        private const string BulkSector = "w";

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

            await _unitOfWork.BeginTransactionAsync();
            try
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

                var result = new ReportDefectResultDto { DefectiveQuantityDeducted = defectiveQuantity };

                if (replacementNeeded > 0)
                {
                    // 2. Standard picking zones only — never the bulk sector, never the shelf we just wrote off.
                    var candidateStocks = await _unitOfWork.Stocks.GetReplacementCandidatesAsync(
                        taskItem.ProductId, taskItem.LocationId, BulkSector);

                    var remaining = replacementNeeded;
                    var picks = new List<(Stock Stock, string ZoneCode, int Quantity)>();

                    foreach (var stock in candidateStocks)
                    {
                        if (remaining == 0) break;

                        var take = Math.Min(remaining, stock.AvailableQuantity);
                        picks.Add((stock, stock.Location!.ZoneCode, take));
                        remaining -= take;
                    }

                    foreach (var pick in picks)
                    {
                        pick.Stock.ReservedQuantity += pick.Quantity;
                    }

                    // 2a. Same zone as the current task: append a new line to it.
                    foreach (var pick in picks.Where(p => p.ZoneCode == task.Sector))
                    {
                        task.Items.Add(new PickTaskItem
                        {
                            PickTaskId = task.Id,
                            ProductId = taskItem.ProductId,
                            LocationId = pick.Stock.LocationId,
                            RequiredQuantity = pick.Quantity,
                            PickedQuantity = 0
                        });
                        result.AppendedToCurrentTaskQuantity += pick.Quantity;
                    }

                    // 2b. Different standard zone: a brand new PickTask targeted at that zone.
                    foreach (var zoneGroup in picks.Where(p => p.ZoneCode != task.Sector).GroupBy(p => p.ZoneCode))
                    {
                        var newTask = new PickTask
                        {
                            OrderId = task.OrderId,
                            Sector = zoneGroup.Key,
                            Status = PickTaskStatus.New,
                            Items = zoneGroup.Select(p => new PickTaskItem
                            {
                                ProductId = taskItem.ProductId,
                                LocationId = p.Stock.LocationId,
                                RequiredQuantity = p.Quantity,
                                PickedQuantity = 0
                            }).ToList()
                        };

                        _unitOfWork.PickTasks.Add(newTask);
                        result.NewPickTaskIds.Add(newTask.Id);
                    }

                    // 2c. Whatever is still short only exists in bulk (or nowhere) — do not hand it
                    //     to a picker. Flag the order line for replenishment instead.
                    if (remaining > 0)
                    {
                        result.ShortageQuantity = remaining;

                        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(task.OrderId);

                        var orderItem = order?.Items.FirstOrDefault(oi => oi.ProductId == taskItem.ProductId);
                        if (orderItem != null)
                        {
                            orderItem.IsPendingReplenishment = true;
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                result.Message = BuildDefectMessage(result);
                return Result<ReportDefectResultDto>.Success(result);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static string BuildDefectMessage(ReportDefectResultDto result)
        {
            var parts = new List<string> { $"{result.DefectiveQuantityDeducted} defective unit(s) written off." };

            if (result.AppendedToCurrentTaskQuantity > 0)
                parts.Add($"{result.AppendedToCurrentTaskQuantity} unit(s) added to your current task from the same zone.");

            if (result.NewPickTaskIds.Count > 0)
                parts.Add($"{result.NewPickTaskIds.Count} new pick task(s) created in other zones.");

            if (result.ShortageQuantity > 0)
                parts.Add($"{result.ShortageQuantity} unit(s) could not be sourced from standard zones and were marked pending replenishment.");

            return string.Join(' ', parts);
        }

        private PickTaskResponseDto MapToDto(PickTask task)
        {
            return new PickTaskResponseDto
            {
                Id = task.Id,
                Sector = task.Sector,
                Status = task.Status.ToString(),
                AssignedWorkerId = task.AssignedWorkerId,
                // The client shows this barcode as the container to scan on completion
                ContainerBarcode = task.Container?.Barcode,
                Items = task.Items.Select(i => new PickTaskItemResponseDto
                {
                    Id = i.Id,
                    ProductName = i.Product!.Name,
                    ProductSku = i.Product.Sku,
                    LocationBarcode = i.Location!.AddressBarcode,
                    RequiredQuantity = i.RequiredQuantity,
                    PickedQuantity = i.PickedQuantity,
                    AvailableStock = i.Location.Stocks
                        .FirstOrDefault(s => s.ProductId == i.ProductId)?.AvailableQuantity ?? 0
                }).ToList()
            };
        }
    }
}
