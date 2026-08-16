// Mirrors Warehouse.Domain.PickTaskStatus (serialized via JsonStringEnumConverter).
export type PickTaskStatus = 'New' | 'InProgress' | 'Completed' | 'Canceled';

export interface PickTaskItem {
    id: string;
    productName: string;
    productSku: string;
    locationBarcode: string;
    requiredQuantity: number;
    pickedQuantity: number;
    missingQuantity: number;
    availableStock: number;
}

export interface PickTask {
    id: string;
    sector: string;
    status: PickTaskStatus;
    items: PickTaskItem[];
    containerBarcode?: string | null;
}