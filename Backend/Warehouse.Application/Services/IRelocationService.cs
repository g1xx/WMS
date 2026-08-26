using Warehouse.Application.Common;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Services;

// Moving stock between locations, modelled as two ordinary stock movements through a
// per-worker transit location:
//
//     source location -> TRANSIT-{worker}    (taking)
//     TRANSIT-{worker} -> target location    (putting away)
//
// Keeping "what a worker is carrying" as normal Stock rows means Stock stays the one
// source of truth for where every unit is, StockTransactions records both legs for free,
// and every existing check and lock applies unchanged. A separate relocation table would
// duplicate that state and drift out of sync with it.
public interface IRelocationService
{
    // What this worker is carrying, creating their transit location on first call.
    Task<RelocationStateDto> GetStateAsync(string workerId, string displayName);

    // Everything currently at a location — backs "press Enter with no product scanned to
    // list what's here and pick from it".
    Task<Result<LocationContentsDto>> GetLocationContentsAsync(string locationBarcode);

    Task<Result<RelocationStateDto>> TakeAsync(string workerId, string displayName, RelocationTakeDto dto);

    Task<Result<RelocationStateDto>> PutAwayAsync(string workerId, string displayName, RelocationPutawayDto dto);
}
