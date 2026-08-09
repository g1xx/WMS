import axiosClient, { fetchSupervisorAuthHeader } from './axiosClient';
import type { PickTask } from '../types/task';

export interface ActionResultMessage {
    message?: string;
}

// Mirrors the original "active-first, then next-in-sector" resolution: from the
// UI's point of view this is ONE read — "what should this worker be looking at
// right now" — so it lives behind a single query function/key, not two.
export async function fetchCurrentPickTask(sector: string): Promise<PickTask | null> {
    const activeResponse = await axiosClient.get(`/PickTask/active?t=${Date.now()}`);
    if (activeResponse.data) {
        return activeResponse.data;
    }

    const nextResponse = await axiosClient.get(`/PickTask/next?sector=${encodeURIComponent(sector)}&t=${Date.now()}`);
    return nextResponse.data ?? null;
}

export async function startPickTask(taskId: string, containerBarcode: string): Promise<void> {
    await axiosClient.post(`/PickTask/${taskId}/start`, { containerBarcode });
}

export async function pickItem(taskId: string, locationBarcode: string, productSku: string, quantity: number): Promise<void> {
    await axiosClient.post(`/PickTask/${taskId}/pick`, { locationBarcode, productSku, quantity });
}

export async function dispatchContainer(taskId: string, containerBarcode: string, conveyorBarcode: string): Promise<ActionResultMessage> {
    const response = await axiosClient.post(`/PickTask/${taskId}/dispatch`, { containerBarcode, conveyorBarcode });
    return response.data;
}

export async function cancelPickTask(taskId: string): Promise<ActionResultMessage> {
    const response = await axiosClient.post(`/PickTask/${taskId}/cancel`);
    return response.data;
}

// Supervisor-gated: exchanges the scanned badge for a short-lived elevated token
// (see fetchSupervisorAuthHeader) and attaches it to this one call only.
export async function reportMissingItem(
    taskId: string,
    locationBarcode: string,
    productSku: string,
    missingQuantity: number,
    supervisorBadge: string
): Promise<ActionResultMessage> {
    const elevatedConfig = await fetchSupervisorAuthHeader(supervisorBadge);
    const response = await axiosClient.post(
        `/PickTask/${taskId}/report-missing`,
        { locationBarcode, productSku, missingQuantity },
        elevatedConfig
    );
    return response.data;
}

// Supervisor-gated: same elevated-token handoff as reportMissingItem above.
export async function reportDefect(
    taskId: string,
    locationBarcode: string,
    productSku: string,
    defectiveQuantity: number,
    supervisorBadge: string
): Promise<ActionResultMessage> {
    const elevatedConfig = await fetchSupervisorAuthHeader(supervisorBadge);
    const response = await axiosClient.post(
        `/PickTask/${taskId}/report-defect`,
        { locationBarcode, productSku, defectiveQuantity },
        elevatedConfig
    );
    return response.data;
}
