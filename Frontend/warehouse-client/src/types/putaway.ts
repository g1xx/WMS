// Mirrors Warehouse.Domain.PutawayTaskStatus (serialized via JsonStringEnumConverter).
export type PutawayTaskStatus = 'New' | 'InProgress' | 'Completed' | 'Canceled';

// Mirrors Warehouse.Application.DTOs.SuggestedPutawayLocationDto. Pre-ranked by the
// backend: same-sector-and-already-stocked, then same-sector-empty-home-slot
// (currentQuantity 0 doesn't mean drop it), then other-sector-informational.
export interface SuggestedPutawayLocation {
    locationBarcode: string;
    currentQuantity: number;
    isInCurrentSector: boolean;
    distinctSkuCount: number;
    maxDistinctSkus: number | null;
}

export interface PutawayTaskItem {
    id: string;
    productName: string;
    productSku: string;
    expectedQuantity: number;
    putAwayQuantity: number;
    missingQuantity: number;
    // A reference for the worker, not a restriction — they can still scan anywhere
    // (see usePutawayWizardSteps's confirmLocation, which just asks to confirm).
    suggestedLocations: SuggestedPutawayLocation[];
}

export interface PutawayTask {
    id: string;
    containerBarcode: string;
    sector: string;
    status: PutawayTaskStatus;
    items: PutawayTaskItem[];
}
