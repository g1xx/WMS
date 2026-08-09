import axiosClient from './axiosClient';
import type { PutawayTask } from '../types/putaway';

export interface ContainerValidation {
    isValid: boolean;
    containerSector: string;
}

export async function fetchActivePutawayTask(): Promise<PutawayTask | null> {
    const response = await axiosClient.get(`/PutawayTask/active?t=${Date.now()}`);
    return response.data ?? null;
}

export async function validateContainer(containerBarcode: string, sector: string): Promise<ContainerValidation> {
    const response = await axiosClient.post('/PutawayTask/validate-container', { containerBarcode, sector });
    return response.data;
}

export async function startPutaway(containerBarcode: string, sector: string): Promise<PutawayTask> {
    const response = await axiosClient.post('/PutawayTask/start', { containerBarcode, sector });
    return response.data;
}

export async function confirmPutawayItem(taskId: string, locationBarcode: string, productSku: string, quantity: number): Promise<PutawayTask> {
    const response = await axiosClient.post(`/PutawayTask/${taskId}/confirm-item`, { locationBarcode, productSku, quantity });
    return response.data;
}

export async function reportPutawayMissing(taskId: string, locationBarcode: string, productSku: string, missingQuantity: number): Promise<PutawayTask> {
    const response = await axiosClient.post(`/PutawayTask/${taskId}/report-missing`, { locationBarcode, productSku, missingQuantity });
    return response.data;
}
