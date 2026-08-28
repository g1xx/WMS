import axiosClient from './axiosClient';
import type { ContainerInfo, LocationInfo, ProductInfo } from '../types/info';

// Read-only lookups backing the "Informacja o..." screen. Staff-only server-side
// (InfoController is AnyStaff) — these expose warehouse layout and stock positions, which
// the Integration feed deliberately cannot reach.

// By SKU only: Product has no barcode column, and Sku is the scanned identifier
// everywhere else in the app.
export async function fetchProductInfo(sku: string): Promise<ProductInfo> {
    const response = await axiosClient.get<ProductInfo>(`/Info/product/${encodeURIComponent(sku)}`);
    return response.data;
}

export async function fetchContainerInfo(barcode: string): Promise<ContainerInfo> {
    const response = await axiosClient.get<ContainerInfo>(`/Info/container/${encodeURIComponent(barcode)}`);
    return response.data;
}

export async function fetchLocationInfo(barcode: string): Promise<LocationInfo> {
    const response = await axiosClient.get<LocationInfo>(`/Info/location/${encodeURIComponent(barcode)}`);
    return response.data;
}
