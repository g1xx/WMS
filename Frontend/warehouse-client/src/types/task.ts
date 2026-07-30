export interface PickTaskItem {
    id: string;
    productName: string;
    productSku: string;
    locationBarcode: string;
    requiredQuantity: number;
    pickedQuantity: number;
    availableStock: number;
}

export interface PickTask {
    id: string;
    sector: string;
    status: string;
    items: PickTaskItem[];
    containerBarcode?: string | null;
}