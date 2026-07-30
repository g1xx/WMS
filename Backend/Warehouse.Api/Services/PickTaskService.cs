using Microsoft.EntityFrameworkCore;
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
                    Items = t.Items.Select(i => new PickTaskItemResponseDto
                    {
                        Id = i.Id,
                        LocationBarcode = i.Location!.AddressBarcode,
                        ProductName = i.Product!.Name,
                        ProductSku = i.Product.Sku,
                        RequiredQuantity = i.RequiredQuantity,
                        PickedQuantity = i.PickedQuantity
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<PickTaskResponseDto?> GetNextTaskAsync(string userId)
        {
            var inProgressTask = await _context.PickTasks
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.AssignedWorkerId == userId && t.Status == PickTaskStatus.InProgress);

            if (inProgressTask != null)
            {
                return MapToDto(inProgressTask);
            }

            var nextTask = await _context.PickTasks
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(t => t.Status == PickTaskStatus.New && t.AssignedWorkerId == null);

            if (nextTask == null) return null;

            return MapToDto(nextTask);
        }

        public async Task<string> StartPickTaskAsync(Guid id, StartPickTaskDto dto, string userId)
        {
            var task = await _context.PickTasks.FindAsync(id);
            if (task == null) throw new KeyNotFoundException("Pick task not found.");

            if (!string.IsNullOrEmpty(task.AssignedWorkerId) && task.AssignedWorkerId != userId)
                throw new InvalidOperationException("Error! This task is already being performed by another worker.");

            if (task.Status == PickTaskStatus.Completed)
                throw new InvalidOperationException("This task has already been fully picked.");

            var container = await _context.Containers
                .FirstOrDefaultAsync(c => c.Barcode == dto.ContainerBarcode);

            if (container == null)
                throw new InvalidOperationException($"Container with barcode '{dto.ContainerBarcode}' not found.");

            // Проверяем, не занят ли контейнер другим заданием
            bool isContainerInUse = await _context.PickTasks
                .AnyAsync(t => t.ContainerId == container.Id && t.Status == PickTaskStatus.InProgress);

            if (isContainerInUse)
            {
                throw new InvalidOperationException($"Ошибка! Контейнер '{dto.ContainerBarcode}' уже используется. Возьмите пустую тару.");
            }

            task.Status = PickTaskStatus.InProgress;
            task.AssignedWorkerId = userId;
            task.ContainerId = container.Id;

            // ---> НОВОЕ: Переводим сам ЗАКАЗ в статус "В сборке" <---
            var order = await _context.Orders.FindAsync(task.OrderId);
            if (order != null && order.Status != OrderStatus.Picking)
            {
                order.Status = OrderStatus.Picking;
            }

            await _context.SaveChangesAsync();

            return "Picking successfully started. Container linked, task locked to you.";
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

            // 1. Обновляем счетчик в задании
            taskItem.PickedQuantity += dto.Quantity;

            // ---> НОВОЕ: 2. Обновляем счетчик в оригинальном заказе (OrderItem) <---
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
                    throw new InvalidOperationException("Неверный штрихкод контейнера! Отсканируйте коробку, привязанную к этому заданию.");
                }

                var station = await _context.Locations.FirstOrDefaultAsync(l => l.AddressBarcode == dto.ConveyorBarcode);
                if (station == null)
                    throw new InvalidOperationException($"Конвейер '{dto.ConveyorBarcode}' не найден.");

                container.LocationId = station.Id;

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

                task.Status = PickTaskStatus.Completed;

                // ---> НОВОЕ: Проверяем, собран ли весь заказ целиком <---
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == task.OrderId);

                if (order != null)
                {
                    // Если во всем заказе больше нет несобранных позиций
                    bool isOrderFullyPicked = order.Items.All(i => i.PickedQuantity >= i.RequiredQuantity);
                    if (isOrderFullyPicked)
                    {
                        order.Status = OrderStatus.Completed; // Если у тебя другой Enum для финиша, поменяй здесь
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

        private PickTaskResponseDto MapToDto(PickTask task)
        {
            return new PickTaskResponseDto
            {
                Id = task.Id,
                Sector = task.Sector,
                Status = task.Status.ToString(),
                Items = task.Items.Select(i => new PickTaskItemResponseDto
                {
                    Id = i.Id,
                    ProductName = i.Product!.Name,
                    ProductSku = i.Product.Sku,
                    LocationBarcode = i.Location!.AddressBarcode,
                    RequiredQuantity = i.RequiredQuantity,
                    PickedQuantity = i.PickedQuantity
                }).ToList()
            };
        }
    }
}