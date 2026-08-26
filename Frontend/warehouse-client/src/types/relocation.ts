export interface RelocationStockLine {
    productSku: string;
    productName: string;
    physicalQuantity: number;
    reservedQuantity: number;
    // What the quantity input defaults to and the most that may be moved. On a shelf
    // that's physical minus what a pick task has reserved; in transit nothing is ever
    // reserved, so it equals the carried amount.
    availableQuantity: number;
}

export interface RelocationState {
    transitBarcode: string;
    carriedItems: RelocationStockLine[];
    // False whenever anything is still carried — a worker cannot walk away holding stock.
    canExit: boolean;
}

export interface LocationContents {
    locationBarcode: string;
    items: RelocationStockLine[];
}
