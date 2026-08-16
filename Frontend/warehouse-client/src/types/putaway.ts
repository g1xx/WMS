// Mirrors Warehouse.Domain.PutawayTaskStatus (serialized via JsonStringEnumConverter).
export type PutawayTaskStatus = 'New' | 'InProgress' | 'Completed' | 'Canceled';

export interface PutawayTaskItem {
    id: string;
    productName: string;
    productSku: string;
    expectedQuantity: number;
    putAwayQuantity: number;
    missingQuantity: number;
    // Address barcodes of locations where this product is already physically
    // stocked — a suggestion for the worker, not a restriction on where it can go.
    suggestedLocationBarcodes: string[];
}

export interface PutawayTask {
    id: string;
    containerBarcode: string;
    sector: string;
    status: PutawayTaskStatus;
    items: PutawayTaskItem[];
}
