export interface PutawayItemRow {
    id: string;
    productSku: string;
    expectedQuantity: number;
}

export interface CreatePutawayItemPayload {
    productSku: string;
    expectedQuantity: number;
}

// The destination for each item is no longer picked here — the warehouse worker
// chooses it during putaway, from locations the system suggests. This request only
// says which zone the resulting task should be routed to.
export interface CreatePutawayPayload {
    containerBarcode: string;
    sector: string;
    items: CreatePutawayItemPayload[];
}

export interface CreatedPutawayTask {
    id: string;
    containerBarcode: string;
    sector: string;
    status: string;
}
