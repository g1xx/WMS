// Mirrors Warehouse.Application.DTOs.OrderableProductDto, served by
// GET /api/Products/for-ordering — the Integration-scoped catalogue view.
//
// This used to be the staff catalogue (GET /api/Products), which carried a per-location
// stocks[] breakdown that this app summed itself. That endpoint is staff-only now, and the
// replacement deliberately does NOT expose warehouse layout: an upstream ERP needs to name
// what it is ordering and know how much it may order, not learn which shelf holds it.
export interface Product {
    id: string;
    name: string;
    sku: string;
    // Already summed across locations server-side, and already net of reservations.
    availableQuantity: number;
}

// Kept as a function rather than inlining the field at every call site: this was a real
// sum over stocks[] before the server started doing it, and the call sites read the same.
export function getAvailableQuantity(product: Product): number {
    return product.availableQuantity;
}
