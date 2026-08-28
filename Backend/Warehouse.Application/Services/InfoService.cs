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

        return Result<ContainerInfoDto>.Success(new ContainerInfoDto
        {
            Barcode = container.Barcode,
            Type = container.Type.ToString(),
            Status = container.Status.ToString(),
            LocationBarcode = container.Location?.AddressBarcode,
            AssignedSector = container.AssignedSector,
            LinkedTask = await ResolveLinkedTaskAsync(container.Id),

            // See ContainerInfoDto.ContentsAvailable: contents aren't modelled as Stock and
            // deriving them from task lines is deferred.
            ContentsAvailable = false
        });
    }

    // A container is held by at most one flow at a time: picking claims it from Available,
    // putaway claims it from Ready. Picking is checked first only because its claim is the
    // narrower one (a single InProgress task); a container can have several pending putaway
    // tasks, of which the first is reported.
    private async Task<ContainerLinkedTaskDto?> ResolveLinkedTaskAsync(Guid containerId)
    {
        var pickTask = await _unitOfWork.PickTasks.GetInProgressForContainerAsync(containerId);
        if (pickTask != null)
        {
            return new ContainerLinkedTaskDto
            {
                Kind = "Picking",
                TaskId = pickTask.Id,
                Status = pickTask.Status.ToString(),
                Sector = pickTask.Sector
            };
        }

        var putawayTasks = await _unitOfWork.PutawayTasks.GetPendingForContainerAsync(containerId);
        var putawayTask = putawayTasks.FirstOrDefault();
        if (putawayTask != null)
        {
            return new ContainerLinkedTaskDto
            {
                Kind = "Putaway",
                TaskId = putawayTask.Id,
                Status = putawayTask.Status.ToString(),
                Sector = putawayTask.Sector
            };
        }

        return null;
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
