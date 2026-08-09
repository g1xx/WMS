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

    void Add(Stock stock);
}
