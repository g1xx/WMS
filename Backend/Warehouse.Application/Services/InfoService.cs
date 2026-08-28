using Warehouse.Application.Common;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

public class InfoService : IInfoService
{
    private readonly IUnitOfWork _unitOfWork;

    public InfoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductInfoDto>> GetProductInfoAsync(string sku)
    {
        // Reuses the batched lookup with a single SKU rather than adding a near-duplicate
        // single-SKU repository method — one query either way.
        var bySku = await _unitOfWork.Products.GetBySkusAsync(new List<string> { sku });
        if (!bySku.TryGetValue(sku, out var product))
            return Result<ProductInfoDto>.Failure($"Product '{sku}' was not found.", ResultErrorType.NotFound);

        var stocks = await _unitOfWork.Stocks.GetByProductWithLocationAsync(product.Id);

        // Split rather than filtered: transit rows leave the addressable list but still
        // count toward the total, so the numbers on screen reconcile with reality.
        var transitRows = stocks.Where(s => s.Location?.Type == LocationType.Transit).ToList();
        var physicalRows = stocks.Where(s => s.Location?.Type != LocationType.Transit).ToList();

        return Result<ProductInfoDto>.Success(new ProductInfoDto
        {
            Sku = product.Sku,
            Name = product.Name,
            WeightKg = product.WeightKg,
            LengthCm = product.LengthCm,
            WidthCm = product.WidthCm,
            HeightCm = product.HeightCm,
            SizeCategory = product.SizeCategory.ToString(),

            // No quantity filter — a row at zero is this SKU's empty home slot and is
            // exactly what someone looking the product up wants to see.
            Locations = physicalRows.Select(s => new ProductLocationLineDto
            {
                LocationBarcode = s.Location?.AddressBarcode ?? "NO_LOCATION",
                LocationType = s.Location?.Type.ToString() ?? "Unknown",
                PhysicalQuantity = s.PhysicalQuantity,
                ReservedQuantity = s.ReservedQuantity,
                AvailableQuantity = s.AvailableQuantity
            }).ToList(),

            CarriedByWorkersQuantity = transitRows.Sum(s => s.PhysicalQuantity)
        });
    }

    public async Task<Result<ContainerInfoDto>> GetContainerInfoAsync(string barcode)
    {
        var container = await _unitOfWork.Containers.GetByBarcodeWithLocationAsync(barcode);
        if (container == null)
            return Result<ContainerInfoDto>.Failure($"Container '{barcode}' was not found.", ResultErrorType.NotFound);

        var pickTask = await _unitOfWork.PickTasks.GetInProgressForContainerAsync(container.Id);
        var putawayTasks = await _unitOfWork.PutawayTasks.GetPendingWithItemsForContainerAsync(container.Id);

        var linkedTasks = new List<ContainerLinkedTaskDto>();
        if (pickTask != null)
        {
            linkedTasks.Add(new ContainerLinkedTaskDto
            {
                Kind = "Picking",
                TaskId = pickTask.Id,
                Status = pickTask.Status.ToString(),
                Sector = pickTask.Sector
            });
        }
        // All of them, not the first: a container legitimately has one putaway task per
        // zone when its expected items span several.
        linkedTasks.AddRange(putawayTasks.Select(t => new ContainerLinkedTaskDto
        {
            Kind = "Putaway",
            TaskId = t.Id,
            Status = t.Status.ToString(),
            Sector = t.Sector
        }));

        return Result<ContainerInfoDto>.Success(new ContainerInfoDto
        {
            Barcode = container.Barcode,
            Type = container.Type.ToString(),
            Status = container.Status.ToString(),
            LocationBarcode = container.Location?.AddressBarcode,
            AssignedSector = container.AssignedSector,
            LinkedTasks = linkedTasks,
            ContentSections = await BuildContentSectionsAsync(container, pickTask, putawayTasks)
        });
    }

    // Contents are derived from task lines — Stock carries no ContainerId — so this returns
    // independently-sourced sections rather than one merged answer, and never subtracts one
    // from another. See ContainerContentSectionDto.
    private async Task<List<ContainerContentSectionDto>> BuildContentSectionsAsync(
        Container container, PickTask? pickTask, List<PutawayTask> putawayTasks)
    {
        // Emptiness is STORED, not derived. ReleaseContainerIfFullyProcessedAsync moves a
        // container to Available exactly when all putaway work for it has finished, and
        // Available is defined as the free pool. Deriving "empty" from task lines instead
        // would re-derive what the status already records, and would additionally read a
        // brand-new container's ABSENCE of task lines as emptiness — which is a different
        // claim entirely.
        if (container.Status == ContainerTransitions.FreeStatus)
            return new List<ContainerContentSectionDto> { new() { Kind = "Empty" } };

        var sections = new List<ContainerContentSectionDto>();

        // Live and exact: these units were scanned into this container and nothing has
        // taken them out.
        if (pickTask != null)
        {
            var lines = pickTask.Items
                .Where(i => i.PickedQuantity > 0)
                .Select(i => new ContainerContentLineDto
                {
                    ProductSku = i.Product?.Sku ?? "?",
                    ProductName = i.Product?.Name ?? "?",
                    Quantity = i.PickedQuantity
                }).ToList();

            sections.Add(new ContainerContentSectionDto
            {
                Kind = "BeingPickedInto",
                Lines = lines,
                SourceTaskId = pickTask.Id,
                Sector = pickTask.Sector
            });
        }

        // Live and exact for what is still to come out. One section per task, because one
        // container can carry a task per zone.
        foreach (var task in putawayTasks)
        {
            var lines = task.Items
                .Select(i => new
                {
                    i.Product,
                    Outstanding = i.ExpectedQuantity - i.PutAwayQuantity - i.MissingQuantity
                })
                .Where(x => x.Outstanding > 0)
                .Select(x => new ContainerContentLineDto
                {
                    ProductSku = x.Product?.Sku ?? "?",
                    ProductName = x.Product?.Name ?? "?",
                    Quantity = x.Outstanding
                }).ToList();

            sections.Add(new ContainerContentSectionDto
            {
                Kind = "ToBePutAway",
                Lines = lines,
                SourceTaskId = task.Id,
                Sector = task.Sector
            });
        }

        // History, not inventory. Only when no pick task currently holds the container —
        // an active one supersedes whatever a previous one left. Deliberately NOT gated on
        // status == Ready, so it still appears alongside a putaway in progress: that is the
        // picked-then-partly-put-away case, and the two facts stand side by side rather
        // than being reconciled into one.
        if (pickTask == null)
        {
            var dispatched = await _unitOfWork.PickTasks.GetMostRecentCompletedForContainerAsync(container.Id);
            if (dispatched != null)
            {
                var lines = dispatched.Items
                    .Where(i => i.PickedQuantity > 0)
                    .Select(i => new ContainerContentLineDto
                    {
                        ProductSku = i.Product?.Sku ?? "?",
                        ProductName = i.Product?.Name ?? "?",
                        Quantity = i.PickedQuantity
                    }).ToList();

                if (lines.Count > 0)
                {
                    sections.Add(new ContainerContentSectionDto
                    {
                        Kind = "AsDispatched",
                        Lines = lines,
                        SourceTaskId = dispatched.Id,
                        Sector = dispatched.Sector,
                        IsHistorical = true
                    });
                }
            }
        }

        // Absence of task lines is not emptiness. A container nothing has ever been
        // recorded against is unknown, and saying "empty" here would be inventing a fact.
        if (sections.Count == 0)
            sections.Add(new ContainerContentSectionDto { Kind = "Unknown" });

        return sections;
    }

    public async Task<Result<LocationInfoDto>> GetLocationInfoAsync(string barcode)
    {
        var location = await _unitOfWork.Locations.GetByBarcodeAsync(barcode);
        if (location == null)
            return Result<LocationInfoDto>.Failure($"Location '{barcode}' was not found.", ResultErrorType.NotFound);

        var stocks = await _unitOfWork.Stocks.GetWithProductAtLocationAsync(location.Id);
        var distinctSkuCount = await _unitOfWork.Stocks.CountDistinctProductsWithStockAtLocationAsync(location.Id);

        return Result<LocationInfoDto>.Success(new LocationInfoDto
        {
            Barcode = location.AddressBarcode,
            Type = location.Type.ToString(),
            Sector = location.Sector,
            ZoneCode = location.ZoneCode,

            Items = stocks.Select(s => new LocationStockLineDto
            {
                ProductSku = s.Product!.Sku,
                ProductName = s.Product!.Name,
                PhysicalQuantity = s.PhysicalQuantity,
                ReservedQuantity = s.ReservedQuantity,
                AvailableQuantity = s.AvailableQuantity
            }).ToList(),

            DistinctSkuCount = distinctSkuCount,
            // The per-row override if set, otherwise the LocationType default — the same
            // resolution StockPlacementService enforces, so the number shown here is the
            // number a putaway will actually be checked against.
            MaxDistinctSkus = location.MaxDistinctSkus
                              ?? LocationCapacityDefaults.GetDefaultMaxDistinctSkus(location.Type)
        });
    }
}
