import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient, { extractErrorMessage } from '../../api/axiosClient';

interface ProductStock {
    productId: string;
    locationBarcode: string;
    quantity: number;
}

interface Product {
    id: string;
    name: string;
    sku: string;
    stocks: ProductStock[];
}

// Mirrors Warehouse.Application.DTOs.StockAdjustmentResultDto.
interface StockAdjustmentResult {
    productId: string;
    locationBarcode: string;
    quantityDelta: number;
    newPhysicalQuantity: number;
    reason: string;
    reservedQuantityReduced: number;
}

// Mirrors Warehouse.Application.DTOs.ProductResponseDto.
interface CreateProductResult {
    id: string;
    name: string;
    sku: string;
    sizeCategory: string;
    stocks: ProductStock[];
}

function getAvailableQuantity(product: Product): number {
    return product.stocks.reduce((sum, s) => sum + s.quantity, 0);
}

// The backend returns 409 specifically when an adjustment would eat into stock already
// reserved for an allocated order — every other rejection (validation, not-found) is a
// 400/404. That's the signal to offer "confirm and apply anyway" instead of just an error.
function isReservationImpactConflict(error: unknown): boolean {
    return (error as { response?: { status?: number } })?.response?.status === 409;
}

const inputStyle: React.CSSProperties = {
    width: '100%',
    padding: '10px',
    boxSizing: 'border-box',
    marginBottom: '12px',
    borderRadius: '4px',
    border: '1px solid #555',
    backgroundColor: '#333',
    color: 'white',
    fontSize: '1rem'
};

const labelStyle: React.CSSProperties = {
    display: 'block',
    color: '#aaa',
    fontSize: '0.85rem',
    marginBottom: '4px'
};

export default function InventoryAdmin() {
    const navigate = useNavigate();

    const [products, setProducts] = useState<Product[]>([]);
    const [loadingProducts, setLoadingProducts] = useState<boolean>(true);

    // --- Adjust stock form ---
    const [adjustProductId, setAdjustProductId] = useState<string>('');
    const [adjustLocationBarcode, setAdjustLocationBarcode] = useState<string>('');
    const [adjustDelta, setAdjustDelta] = useState<number>(0);
    const [adjustReason, setAdjustReason] = useState<string>('');
    const [adjustSubmitting, setAdjustSubmitting] = useState<boolean>(false);
    const [adjustMessage, setAdjustMessage] = useState<string>('');
    // Set only by a 409 (reservation-impact conflict) — offers a distinct "confirm and
    // apply anyway" action instead of just showing an error. Cleared whenever any field
    // changes, so a stale confirmation can never silently apply to different values.
    const [adjustConflictMessage, setAdjustConflictMessage] = useState<string>('');

    const clearAdjustConflict = () => {
        if (adjustConflictMessage) setAdjustConflictMessage('');
    };

    // --- Create product form ---
    const [newName, setNewName] = useState<string>('');
    const [newSku, setNewSku] = useState<string>('');
    const [newPrice, setNewPrice] = useState<number>(0);
    const [newWeightKg, setNewWeightKg] = useState<number>(0);
    const [newLengthCm, setNewLengthCm] = useState<number>(0);
    const [newWidthCm, setNewWidthCm] = useState<number>(0);
    const [newHeightCm, setNewHeightCm] = useState<number>(0);
    const [newBaseUnit, setNewBaseUnit] = useState<number>(0);
    const [newItemPerPackage, setNewItemPerPackage] = useState<number>(1);
    const [newLocationBarcode, setNewLocationBarcode] = useState<string>('');
    const [newSector, setNewSector] = useState<string>('');
    const [newWarehouseCode, setNewWarehouseCode] = useState<string>('');
    const [newFloor, setNewFloor] = useState<number>(1);
    const [newInitialQuantity, setNewInitialQuantity] = useState<number>(0);
    const [createSubmitting, setCreateSubmitting] = useState<boolean>(false);
    const [createMessage, setCreateMessage] = useState<string>('');

    useEffect(() => {
        void loadProducts();
    }, []);

    const loadProducts = async () => {
        setLoadingProducts(true);
        try {
            const response = await axiosClient.get<Product[]>('/Products');
            setProducts(response.data);
        } catch (error) {
            console.error('Failed to load products:', error);
        } finally {
            setLoadingProducts(false);
        }
    };

    const handleAdjustSubmit = async (confirmReservationImpact: boolean) => {
        if (!adjustProductId || !adjustLocationBarcode.trim() || adjustDelta === 0 || !adjustReason.trim()) {
            setAdjustMessage('Product, location, a non-zero quantity delta, and a reason are all required.');
            return;
        }

        setAdjustSubmitting(true);
        setAdjustMessage('');
        setAdjustConflictMessage('');
        try {
            const response = await axiosClient.post<StockAdjustmentResult>('/Inventory/adjust-stock', {
                productId: adjustProductId,
                locationBarcode: adjustLocationBarcode.trim(),
                quantityDelta: adjustDelta,
                reason: adjustReason.trim(),
                confirmReservationImpact
            });
            const reservedQuantityReduced = response.data?.reservedQuantityReduced ?? 0;
            setAdjustMessage(
                reservedQuantityReduced > 0
                    ? `Done. New physical quantity: ${response.data?.newPhysicalQuantity ?? '?'}. ` +
                      `WARNING: this also released ${reservedQuantityReduced} reserved unit(s) — check which order(s) at this location are now short.`
                    : `Done. New physical quantity: ${response.data?.newPhysicalQuantity ?? '?'}.`
            );
            setAdjustDelta(0);
            setAdjustReason('');
            await loadProducts();
        } catch (error) {
            if (isReservationImpactConflict(error)) {
                setAdjustConflictMessage(extractErrorMessage(error, 'This would reduce stock already reserved for an order.'));
            } else {
                setAdjustMessage(extractErrorMessage(error, 'Failed to adjust stock.'));
            }
        } finally {
            setAdjustSubmitting(false);
        }
    };

    const handleCreateSubmit = async () => {
        if (!newName.trim() || !newSku.trim() || !newLocationBarcode.trim() || !newSector.trim() || !newWarehouseCode.trim()) {
            setCreateMessage('Name, SKU, location barcode, sector, and warehouse code are all required.');
            return;
        }

        setCreateSubmitting(true);
        setCreateMessage('');
        try {
            const response = await axiosClient.post<CreateProductResult>('/Inventory/products', {
                name: newName.trim(),
                sku: newSku.trim(),
                price: newPrice,
                weightKg: newWeightKg,
                lengthCm: newLengthCm,
                widthCm: newWidthCm,
                heightCm: newHeightCm,
                baseUnit: newBaseUnit,
                itemPerPackage: newItemPerPackage,
                locationBarcode: newLocationBarcode.trim(),
                sector: newSector.trim(),
                warehouseCode: newWarehouseCode.trim(),
                floor: newFloor,
                initialQuantity: newInitialQuantity
            });
            setCreateMessage(`Created "${response.data?.name ?? newName}" with ${newInitialQuantity} unit(s) at ${newLocationBarcode.trim()}.`);
            setNewName('');
            setNewSku('');
            setNewPrice(0);
            setNewWeightKg(0);
            setNewLengthCm(0);
            setNewWidthCm(0);
            setNewHeightCm(0);
            setNewItemPerPackage(1);
            setNewLocationBarcode('');
            setNewSector('');
            setNewWarehouseCode('');
            setNewFloor(1);
            setNewInitialQuantity(0);
            await loadProducts();
        } catch (error) {
            setCreateMessage(extractErrorMessage(error, 'Failed to create product.'));
        } finally {
            setCreateSubmitting(false);
        }
    };

    return (
        <div style={{ backgroundColor: '#121212', minHeight: '100vh', color: '#e0e0e0', padding: '20px' }}>
            <div style={{ maxWidth: '600px', margin: '0 auto' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px' }}>
                    <button
                        onClick={() => navigate('/')}
                        style={{ padding: '8px 14px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                    >
                        &larr; Back to Picking Terminal
                    </button>

                    {/* /inbound is a separate app (the simulated upstream ERP/marketplace
                        order feed), served by nginx alongside this one — a plain anchor,
                        not a router Link, since it's a full navigation across app boundaries. */}
                    <a
                        href="/inbound"
                        style={{ padding: '8px 14px', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '4px', textDecoration: 'none' }}
                    >
                        Inbound Order Feed &rarr;
                    </a>
                </div>

                <h2 style={{ marginBottom: '20px' }}>Admin: Inventory</h2>

                {/* ADJUST STOCK */}
                <section style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', marginBottom: '25px' }}>
                    <h3 style={{ color: '#4CAF50', marginTop: 0 }}>Adjust Stock</h3>

                    <label style={labelStyle}>Product</label>
                    <select value={adjustProductId} onChange={(e) => { setAdjustProductId(e.target.value); clearAdjustConflict(); }} style={inputStyle} disabled={loadingProducts}>
                        <option value="">{loadingProducts ? 'Loading products...' : 'Select a product...'}</option>
                        {products.map(p => (
                            <option key={p.id} value={p.id}>{p.name} ({p.sku}) — available: {getAvailableQuantity(p)}</option>
                        ))}
                    </select>

                    <label style={labelStyle}>Location barcode</label>
                    <input type="text" value={adjustLocationBarcode} onChange={(e) => { setAdjustLocationBarcode(e.target.value); clearAdjustConflict(); }} placeholder="e.g. mp101010101a" style={inputStyle} />

                    <label style={labelStyle}>Quantity delta (positive to add, negative to remove)</label>
                    <input type="number" value={adjustDelta} onChange={(e) => { setAdjustDelta(Number(e.target.value)); clearAdjustConflict(); }} style={inputStyle} />

                    <label style={labelStyle}>Reason</label>
                    <input type="text" value={adjustReason} onChange={(e) => { setAdjustReason(e.target.value); clearAdjustConflict(); }} placeholder="e.g. cycle count correction" style={inputStyle} />

                    <button
                        onClick={() => void handleAdjustSubmit(false)}
                        disabled={adjustSubmitting}
                        style={{ width: '100%', padding: '14px', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '4px', cursor: adjustSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                    >
                        {adjustSubmitting ? 'Submitting...' : 'Apply Adjustment'}
                    </button>

                    {adjustConflictMessage && (
                        <div style={{ marginTop: '12px', padding: '12px', borderRadius: '4px', backgroundColor: '#4a1f1f', border: '1px solid #ff5252' }}>
                            <p style={{ margin: '0 0 10px 0', color: '#ff8a80' }}>{adjustConflictMessage}</p>
                            <button
                                onClick={() => void handleAdjustSubmit(true)}
                                disabled={adjustSubmitting}
                                style={{ width: '100%', padding: '12px', backgroundColor: '#ff5252', color: 'white', border: 'none', borderRadius: '4px', cursor: adjustSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                            >
                                {adjustSubmitting ? 'Submitting...' : 'Confirm and Apply Anyway'}
                            </button>
                        </div>
                    )}

                    {adjustMessage && <p style={{ marginTop: '12px', color: '#ffeb3b' }}>{adjustMessage}</p>}
                </section>

                {/* CREATE PRODUCT */}
                <section style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px' }}>
                    <h3 style={{ color: '#4CAF50', marginTop: 0 }}>Create New Product</h3>

                    <label style={labelStyle}>Name</label>
                    <input type="text" value={newName} onChange={(e) => setNewName(e.target.value)} style={inputStyle} />

                    <label style={labelStyle}>SKU</label>
                    <input type="text" value={newSku} onChange={(e) => setNewSku(e.target.value)} style={inputStyle} />

                    <div style={{ display: 'flex', gap: '10px' }}>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Price</label>
                            <input type="number" value={newPrice} onChange={(e) => setNewPrice(Number(e.target.value))} style={inputStyle} />
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Weight (kg)</label>
                            <input type="number" value={newWeightKg} onChange={(e) => setNewWeightKg(Number(e.target.value))} style={inputStyle} />
                        </div>
                    </div>

                    <div style={{ display: 'flex', gap: '10px' }}>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Length (cm)</label>
                            <input type="number" value={newLengthCm} onChange={(e) => setNewLengthCm(Number(e.target.value))} style={inputStyle} />
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Width (cm)</label>
                            <input type="number" value={newWidthCm} onChange={(e) => setNewWidthCm(Number(e.target.value))} style={inputStyle} />
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Height (cm)</label>
                            <input type="number" value={newHeightCm} onChange={(e) => setNewHeightCm(Number(e.target.value))} style={inputStyle} />
                        </div>
                    </div>

                    <div style={{ display: 'flex', gap: '10px' }}>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Base unit</label>
                            <select value={newBaseUnit} onChange={(e) => setNewBaseUnit(Number(e.target.value))} style={inputStyle}>
                                <option value={0}>Piece</option>
                                <option value={1}>Package</option>
                            </select>
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Items per package</label>
                            <input type="number" min={1} value={newItemPerPackage} onChange={(e) => setNewItemPerPackage(Number(e.target.value))} style={inputStyle} />
                        </div>
                    </div>

                    <hr style={{ borderColor: '#333', margin: '15px 0' }} />
                    <p style={{ color: '#888', fontSize: '0.85rem', marginTop: 0 }}>Target location (bin/shelf) and starting stock</p>

                    <label style={labelStyle}>Location barcode</label>
                    <input type="text" value={newLocationBarcode} onChange={(e) => setNewLocationBarcode(e.target.value)} placeholder="e.g. mp101010101a" style={inputStyle} />

                    <div style={{ display: 'flex', gap: '10px' }}>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Sector</label>
                            <input type="text" value={newSector} onChange={(e) => setNewSector(e.target.value)} placeholder="e.g. p" style={inputStyle} />
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Warehouse code</label>
                            <input type="text" value={newWarehouseCode} onChange={(e) => setNewWarehouseCode(e.target.value)} placeholder="e.g. m" style={inputStyle} />
                        </div>
                        <div style={{ flex: 1 }}>
                            <label style={labelStyle}>Floor</label>
                            <input type="number" value={newFloor} onChange={(e) => setNewFloor(Number(e.target.value))} style={inputStyle} />
                        </div>
                    </div>

                    <label style={labelStyle}>Initial quantity</label>
                    <input type="number" min={0} value={newInitialQuantity} onChange={(e) => setNewInitialQuantity(Number(e.target.value))} style={inputStyle} />

                    <button
                        onClick={() => void handleCreateSubmit()}
                        disabled={createSubmitting}
                        style={{ width: '100%', padding: '14px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '4px', cursor: createSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                    >
                        {createSubmitting ? 'Creating...' : 'Create Product'}
                    </button>

                    {createMessage && <p style={{ marginTop: '12px', color: '#ffeb3b' }}>{createMessage}</p>}
                </section>
            </div>
        </div>
    );
}
