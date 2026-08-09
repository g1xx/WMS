export interface ProductStock {
    productId: string;
    locationBarcode: string;
    quantity: number;
}

export interface Product {
    id: string;
    name: string;
    sku: string;
    sizeCategory: string;
    stocks: ProductStock[];
}

// A product can be stocked across several locations; AvailableQuantity is the sum across all of them.
export function getAvailableQuantity(product: Product): number {
    return product.stocks.reduce((sum, s) => sum + s.quantity, 0);
}
