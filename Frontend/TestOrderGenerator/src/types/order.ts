export interface OrderItemPayload {
    productId: string;
    requiredQuantity: number;
}

export interface OrderCreatePayload {
    customerName: string;
    destinationAddress: string;
    items: OrderItemPayload[];
}

export interface CreatedOrder {
    id: string;
    orderNumber: string;
    customerName: string;
    destinationAddress: string;
    status: string;
}

export type OrderMode = 'manual' | 'random';
export type LogStatus = 'created' | 'allocated' | 'shortage' | 'error';

export interface LogEntry {
    id: string;
    mode: OrderMode;
    orderNumber?: string;
    status: LogStatus;
    message: string;
}
