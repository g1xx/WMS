using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

public interface IStockRepository
{
    Task<Stock?> GetByProductAndLocationAsync(Guid productId, Guid locationId);

    // Batched (one query for every product) — feeds OrderAllocationService's
    // per-product-id dictionary lookup, avoiding the N+1 this replaced.
    Task<List<Stock>> GetAvailableForProductsAsync(List<Guid> productIds);

    // Defect-replacement candidates: same product, a different location, never the
    // bulk sector, with spare available quantity — used by ReportDefectAsync.
    Task<List<Stock>> GetReplacementCandidatesAsync(Guid productId, Guid excludeLocationId, string excludeSector);

    // Product+Location included — feeds StocksController's full stock listing.
    Task<List<Stock>> GetAllWithDetailsAsync();

    // Batched — every distinct product in a putaway task, mapped to the address
    // barcodes of locations where it's currently physically stocked (PhysicalQuantity > 0).
    // Powers the "suggested locations" a worker sees when choosing where to put an item away.
    Task<Dictionary<Guid, List<string>>> GetLocationBarcodesByProductAsync(List<Guid> productIds);

    void Add(Stock stock);
}
