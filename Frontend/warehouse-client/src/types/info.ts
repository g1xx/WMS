// Read-only lookup screen ("Informacja o..."). Mirrors Warehouse.Application.DTOs.InfoDtos.

export interface ProductLocationLine {
    locationBarcode: string;
    locationType: string;
    physicalQuantity: number;
    reservedQuantity: number;
    availableQuantity: number;
}

export interface ProductInfo {
    sku: string;
    name: string;
    weightKg: number;
    lengthCm: number;
    widthCm: number;
    heightCm: number;
    sizeCategory: string;
    // Includes locations sitting at zero — a SKU's empty home slot is worth seeing.
    // Excludes transit locations; those are summed into carriedByWorkersQuantity instead.
    locations: ProductLocationLine[];
    carriedByWorkersQuantity: number;
}

export interface ContainerLinkedTask {
    kind: string;
    taskId: string;
    status: string;
    sector: string;
}

export type ContainerContentKind =
    | 'Empty'
    | 'BeingPickedInto'
    | 'ToBePutAway'
    | 'AsDispatched'
    | 'Unknown';

export interface ContainerContentLine {
    productSku: string;
    productName: string;
    quantity: number;
}

export interface ContainerContentSection {
    kind: ContainerContentKind;
    lines: ContainerContentLine[];
    sourceTaskId: string | null;
    sector: string | null;
    // True only for AsDispatched. That section describes the PAST and nothing invalidates
    // it, so it must be styled as history — never as a live inventory line.
    isHistorical: boolean;
}

export interface ContainerInfo {
    barcode: string;
    type: string;
    status: string;
    locationBarcode: string | null;
    assignedSector: string | null;
    // Every task holding it — a container can have one putaway task per zone.
    linkedTasks: ContainerLinkedTask[];
    // Independently-sourced views, never merged into one number. See the backend's
    // ContainerContentSectionDto for why.
    contentSections: ContainerContentSection[];
}

export interface LocationStockLine {
    productSku: string;
    productName: string;
    physicalQuantity: number;
    reservedQuantity: number;
    availableQuantity: number;
}

export interface LocationInfo {
    barcode: string;
    type: string;
    sector: string;
    zoneCode: string;
    items: LocationStockLine[];
    distinctSkuCount: number;
    // null means no limit, never zero.
    maxDistinctSkus: number | null;
}
