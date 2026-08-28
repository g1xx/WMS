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

export interface ContainerInfo {
    barcode: string;
    type: string;
    status: string;
    locationBarcode: string | null;
    assignedSector: string | null;
    linkedTask: ContainerLinkedTask | null;
    // Always false for now — container contents aren't modelled as Stock. Render "not
    // available yet", never an empty list, which a worker would read as "it's empty".
    contentsAvailable: boolean;
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
