import axiosClient from './axiosClient';
import type { LocationContents, RelocationState } from '../types/relocation';

// The worker is always taken from the auth token server-side — no id is ever sent, so
// there is no way to address another worker's transit location from here.

export async function fetchRelocationState(): Promise<RelocationState> {
    const response = await axiosClient.get<RelocationState>(`/Relocation/state?t=${Date.now()}`);
    return response.data;
}

export async function fetchLocationContents(locationBarcode: string): Promise<LocationContents> {
    const response = await axiosClient.get<LocationContents>(
        `/Relocation/location/${encodeURIComponent(locationBarcode)}?t=${Date.now()}`);
    return response.data;
}

export async function takeStock(
    sourceLocationBarcode: string, productSku: string, quantity: number,
): Promise<RelocationState> {
    const response = await axiosClient.post<RelocationState>('/Relocation/take', {
        sourceLocationBarcode, productSku, quantity,
    });
    return response.data;
}

export async function putAwayStock(
    targetLocationBarcode: string, productSku: string, quantity: number,
): Promise<RelocationState> {
    const response = await axiosClient.post<RelocationState>('/Relocation/putaway', {
        targetLocationBarcode, productSku, quantity,
    });
    return response.data;
}
