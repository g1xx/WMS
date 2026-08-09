export interface PutawayTaskItem {
    id: string;
    locationBarcode: string;
    productName: string;
    productSku: string;
    expectedQuantity: number;
    putAwayQuantity: number;
    missingQuantity: number;
}

export interface PutawayTask {
    id: string;
    containerBarcode: string;
    sector: string;
    status: string;
    items: PutawayTaskItem[];
}
