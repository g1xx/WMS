export interface Location {
    id: string;
    addressBarcode: string;
    warehouseCode: string;
    sector: string;
    floor: number;
}

// Same WarehouseCode+Sector+Floor convention the backend uses for PutawayTask.Sector —
// lets the form filter "Target Location" choices down to the Assigned Sector typed above.
export function getZoneCode(location: Location): string {
    return `${location.warehouseCode}${location.sector}${location.floor}`;
}

export interface PutawayItemRow {
    id: string;
    locationBarcode: string;
    productSku: string;
    expectedQuantity: number;
}

export interface CreatePutawayItemPayload {
    productSku: string;
    destinationLocationBarcode: string;
    expectedQuantity: number;
}

export interface CreatePutawayPayload {
    containerBarcode: string;
    items: CreatePutawayItemPayload[];
}

export interface CreatedPutawayTask {
    id: string;
    containerBarcode: string;
    sector: string;
    status: string;
}
