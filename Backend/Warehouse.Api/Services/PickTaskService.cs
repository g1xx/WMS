using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Common;
using Warehouse.Api.DTOs;
using Warehouse.Domain;
using Warehouse.Infrastructure;

namespace Warehouse.Application.Services
{
    public class PickTaskService : IPickTaskService
    {
        private readonly AppDbContext _context;

        public PickTaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PickTaskResponseDto>> GetPickTasksAsync()
        {
            return await _context.PickTasks
                .AsNoTracking()
                .Select(t => new PickTaskResponseDto
                {
                    Id = t.Id,
                    Sector = t.Sector,
                    Status = t.Status.ToString(),
                    AssignedWorkerId = t.AssignedWorkerId,
                    ContainerBarcode = t.Container != null ? t.Container.Barcode : null,
                    Items = t.Items.Select(i => new PickTaskItemResponseDto
                    {
                        Id = i.Id,
                        LocationBarcode = i.Location!.AddressBarcode,
                        ProductName = i.Product!.Name,
                        ProductSku = i.Product.Sku,
                        RequiredQuantity = i.RequiredQuantity,
                        PickedQuantity = i.PickedQuantity,
                        AvailableStock = i.Location.Stocks
                            .Where(s => s.ProductId == i.ProductId)
                            .Select(s => s.PhysicalQuantity - s.ReservedQuantity)
                            .FirstOrDefault()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<PickTaskResponseDto?> GetActiveTaskForUserAsync(string userId)
        {
            // Independent of sector on purpose: a worker who gets logged out mid-pick
            // must be able to resume without going through sector selection again.
            var inProgressTask = await _context.PickTasks
                .Include(t => t.Container)
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
                .FirstOrDefaultAsync(t => t.AssignedWorkerId == userId && t.Status == PickTaskStatus.InProgress);

            return inProgressTask == null ? null : MapToDto(inProgressTask);
        }

        public async Task<PickTaskResponseDto?> GetNextTaskAsync(string userId, string sector)
        {
            // Strictly segregated: a worker in zone "mp1" only ever sees tasks whose
            // items physically live in "mp1" (PickTask.Sector holds the zone code).
            var nextTask = await _context.PickTasks
                .Include(t => t.Container)
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location).ThenInclude(l => l!.Stocks)
                .FirstOrDefaultAsync(t => t.Status == PickTaskStatus.New && t.AssignedWorkerId == null && t.Sector == sector);

            return nextTask == null ? null : MapToDto(nextTask);
        }

        public async Task<Result<string>> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId)
        {
            var task = await _context.PickTasks.FindAsync(id);
            if (task == null)
                return Result<string>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (!string.IsNullOrEmpty(task.AssignedWorkerId) && task.AssignedWorkerId != userId)
                return Result<string>.Failure("Error! This task is already being performed by another worker.");

            if (task.Status == PickTaskStatus.Completed)
                return Result<string>.Failure("This task has already been fully picked.");

            // Only ever fetch/suggest containers that are free (New) — an
            // InProgress or Ready container is already committed elsewhere.
            var container = await _context.Containers
                .Where(c => c.Status == ContainerStatus.New)
                .FirstOrDefaultAsync(c => c.Barcode == dto.ContainerBarcode);

            if (container == null)
            {
                // Distinguish "doesn't exist" from "exists but not free" for a clearer message
                var containerExists = await _context.Containers.AnyAsync(c => c.Barcode == dto.ContainerBarcode);

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

            // Move the ORDER itself into the "Picking" status
            var order = await _context.Orders.FindAsync(task.OrderId);
            if (order != null && order.Status != OrderStatus.Picking)
            {
                order.Status = OrderStatus.Picking;
            }

            await _context.SaveChangesAsync();

            return Result<string>.Success("Picking successfully started. Container linked, task locked to you.");
        }

        public async Task<string> PickItemAsync(Guid id, PickItemDto dto, string userId)
        {
            var task = await _context.PickTasks
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) throw new KeyNotFoundException("Task not found.");
            if (task.Status != PickTaskStatus.InProgress) throw new InvalidOperationException("Cannot scan item: task is not active.");
            if (task.AssignedWorkerId != userId) throw new InvalidOperationException("Access error! The task is being performed by another worker.");

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null) throw new InvalidOperationException("Scan error! You are at the wrong location or picked the wrong item.");

            if (taskItem.PickedQuantity + dto.Quantity > taskItem.RequiredQuantity)
            {
                var leftToPick = taskItem.RequiredQuantity - taskItem.PickedQuantity;
                throw new InvalidOperationException($"Over-pick! You only need to pick {leftToPick} more units of this item.");
            }

            // 1. Update the counter on the pick task
            taskItem.PickedQuantity += dto.Quantity;

            // 2. Update the counter on the original order (OrderItem)
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == task.OrderId);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(oi => oi.ProductId == taskItem.ProductId);
                if (orderItem != null)
                {
                    orderItem.PickedQuantity += dto.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            return $"Successfully picked: {dto.Quantity} units.";
        }

        public async Task<Guid?> DispatchContainerAsync(Guid id, DispatchContainerDto dto, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var task = await _context.PickTasks
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null) throw new KeyNotFoundException("Pick task not found.");
                if (task.Status != PickTaskStatus.InProgress) throw new InvalidOperationException("Task is not in progress.");
                if (task.AssignedWorkerId != userId) throw new InvalidOperationException("Task belongs to another worker.");

                var container = await _context.Containers.FindAsync(task.ContainerId);
                if (container == null || container.Barcode != dto.ContainerBarcode)
                {
                    throw new InvalidOperationException("Wrong container barcode! Scan the container linked to this task.");
                }

                var station = await _context.Locations.FirstOrDefaultAsync(l => l.AddressBarcode == dto.ConveyorBarcode);
                if (station == null)
                    throw new InvalidOperationException($"Conveyor '{dto.ConveyorBarcode}' was not found.");

                container.LocationId = station.Id;
                // The pick task closes out here, so the container is done being worked and heads to the conveyor
                container.Status = ContainerStatus.Ready;

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
                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.LocationId == item.LocationId);

                    if (stock != null)
                    {
                        stock.PhysicalQuantity -= item.PickedQuantity;
                        stock.ReservedQuantity -= item.PickedQuantity;
                    }
                }

                task.Status = PickTaskStatus.Completed;

                // Check whether the whole order has now been picked
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == task.OrderId);

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

                    _context.PickTasks.Add(nextTask);
                    await _context.SaveChangesAsync();
                    newTaskId = nextTask.Id;
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return newTaskId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> CancelPickTaskAsync(Guid id, string userId)
        {
            var task = await _context.PickTasks
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) throw new KeyNotFoundException("Pick task not found.");

            if (task.Status != PickTaskStatus.InProgress)
                throw new InvalidOperationException("Only a task that is in progress can be cancelled.");

            // Return the task to its initial state: drop the worker and container, reset progress
            task.Status = PickTaskStatus.New;
            task.AssignedWorkerId = null;
            task.ContainerId = null;

            foreach (var item in task.Items)
            {
                item.PickedQuantity = 0;
            }

            await _context.SaveChangesAsync();

            return "Pick task cancelled and returned to the queue.";
        }

        public async Task<string> ReportMissingItemAsync(Guid taskId, ReportMissingItemDto dto, string workerId)
        {
            var task = await _context.PickTasks
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) throw new KeyNotFoundException("Pick task not found.");

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
                throw new InvalidOperationException("Item not found in this task: wrong location or SKU.");

            // Record what was actually picked: required minus what could not be found
            var actuallyPicked = taskItem.RequiredQuantity - dto.MissingQuantity;
            if (actuallyPicked < 0) actuallyPicked = 0;

            taskItem.PickedQuantity = actuallyPicked;

            // Stage 1: stock reservation adjustments will go here
            task.Status = PickTaskStatus.Completed;

            await _context.SaveChangesAsync();

            return $"Missing item reported. Recorded {actuallyPicked} of {taskItem.RequiredQuantity} units picked.";
        }

        // Bulk/high-rack storage sector (e.g. the "w" in "mw1") — never a valid
        // source for direct picking or defect-replacement reallocation.
        private const string BulkSector = "w";

        public async Task<Result<ReportDefectResultDto>> ReportDefectAsync(Guid taskId, ReportDefectDto dto, string workerId)
        {
            if (dto.DefectiveQuantity <= 0)
                return Result<ReportDefectResultDto>.Failure("Defective quantity must be greater than zero.");

            var task = await _context.PickTasks
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return Result<ReportDefectResultDto>.Failure("Pick task not found.", ResultErrorType.NotFound);

            if (task.Status != PickTaskStatus.InProgress)
                return Result<ReportDefectResultDto>.Failure("Cannot report a defect: task is not active.");

            if (task.AssignedWorkerId != workerId)
                return Result<ReportDefectResultDto>.Failure("Access error! The task is being performed by another worker.");

            var taskItem = task.Items.FirstOrDefault(i =>
                i.Location!.AddressBarcode == dto.LocationBarcode &&
                i.Product!.Sku == dto.ProductSku);

            if (taskItem == null)
                return Result<ReportDefectResultDto>.Failure("Item not found in this task: wrong location or SKU.");

            var sourceStock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == taskItem.ProductId && s.LocationId == taskItem.LocationId);

            if (sourceStock == null)
                return Result<ReportDefectResultDto>.Failure("No stock record found at this location for this product.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Deduct the defective units strictly from PhysicalQuantity. ReservedQuantity
                //    is left untouched here — the reservation this order already holds only moves
                //    once we know where (or whether) it can be replaced below.
                var defectiveQuantity = Math.Min(dto.DefectiveQuantity, sourceStock.PhysicalQuantity);
                sourceStock.PhysicalQuantity -= defectiveQuantity;

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
                    var candidateStocks = await _context.Stocks
                        .Include(s => s.Location)
                        .Where(s => s.ProductId == taskItem.ProductId
                                    && s.LocationId != taskItem.LocationId
                                    && s.Location!.Sector != BulkSector
                                    && (s.PhysicalQuantity - s.ReservedQuantity) > 0)
                        .OrderByDescending(s => s.PhysicalQuantity - s.ReservedQuantity)
                        .ToListAsync();

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

                        _context.PickTasks.Add(newTask);
                        result.NewPickTaskIds.Add(newTask.Id);
                    }

                    // 2c. Whatever is still short only exists in bulk (or nowhere) — do not hand it
                    //     to a picker. Flag the order line for replenishment instead.
                    if (remaining > 0)
                    {
                        result.ShortageQuantity = remaining;

                        var order = await _context.Orders
                            .Include(o => o.Items)
                            .FirstOrDefaultAsync(o => o.Id == task.OrderId);

                        var orderItem = order?.Items.FirstOrDefault(oi => oi.ProductId == taskItem.ProductId);
                        if (orderItem != null)
                        {
                            orderItem.IsPendingReplenishment = true;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Message = BuildDefectMessage(result);
                return Result<ReportDefectResultDto>.Success(result);
            }
            catch
            {
                await transaction.RollbackAsync();
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