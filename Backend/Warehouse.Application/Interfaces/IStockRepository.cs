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

    // Batched — every distinct product in a putaway task, mapped to every location that
    // has (or ever had) a Stock row for it, at whatever quantity it currently holds —
    // including 0. Powers PutawayService's suggested-locations ranking, which needs the
    // zero-quantity rows too (a SKU's "home slot" that's currently empty must still show
    // up, not be silently dropped the way a PhysicalQuantity > 0 filter would).
    Task<Dictionary<Guid, List<PutawaySuggestionCandidate>>> GetPutawaySuggestionCandidatesByProductAsync(List<Guid> productIds);

    // Batched — current distinct-SKU count (PhysicalQuantity > 0 only) for each of the
    // given locations. Feeds the MaxDistinctSkus exclusion when ranking suggestions;
    // for the single-location version used at actual confirm time, see
    // CountDistinctProductsWithStockAtLocationAsync below.
    Task<Dictionary<Guid, int>> GetDistinctSkuCountsByLocationsAsync(List<Guid> locationIds);

    void Add(Stock stock);

    // Distinct products with a physical presence at this location — a SKU whose Stock
    // row has dropped to zero doesn't occupy a slot, so it's excluded here. Feeds the
    // MaxDistinctSkus check in PutawayService.ConfirmItemAsync. Call this only after
    // ILocationRepository.LockForUpdateAsync on the same location, inside the same
    // transaction — otherwise this read isn't protected against a concurrent confirm.
    Task<int> CountDistinctProductsWithStockAtLocationAsync(Guid locationId);
}
