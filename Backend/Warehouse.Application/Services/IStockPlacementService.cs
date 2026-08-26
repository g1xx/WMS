using Warehouse.Application.Common;
using Warehouse.Domain;

namespace Warehouse.Application.Services;

// The destination half of every stock movement: putting units INTO a location.
//
// Extracted from PutawayService.ConfirmItemAsync so relocation's second leg runs the
// identical code rather than a second copy of it. The MaxDistinctSkus check in particular
// must not be duplicated — two implementations of a capacity rule drift, and the one that
// drifts is the one nobody is looking at.
public interface IStockPlacementService
{
    // Places quantity units of productId into the given location, enforcing
    // MaxDistinctSkus and writing the audit row. MUST be called inside a transaction: it
    // takes a row lock on the destination that is only held until the caller's
    // transaction commits.
    //
    // Takes the resolved Location rather than an id because every caller has already
    // looked it up (by the barcode the worker scanned) — re-fetching it here would be a
    // redundant round-trip on the hot path of every scan.
    //
    // Returns a rejection message (the capacity refusal) or null on success. A refusal is
    // a business outcome the worker can act on by choosing another location, not an error.
    // Takes the Product rather than a bare id so the capacity refusal can name the SKU
    // the worker is holding ("doesn't currently stock SKU-1") instead of "this product" —
    // they're standing at a shelf deciding where to put it.
    Task<string?> PlaceAsync(
        Product product,
        Location location,
        int quantity,
        string userId,
        StockTransactionType transactionType);
}
