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

// Mirrors Warehouse.Application.DTOs.OrderCreateResultDto. Allocation now happens
// server-side as part of order creation — the feed integration has no access to the
// separate POST /Orders/{id}/allocate endpoint (see RoleNames.Integration).
export interface OrderCreateResult {
    order: CreatedOrder;
    isAllocated: boolean;
    allocationMessage: string | null;
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
