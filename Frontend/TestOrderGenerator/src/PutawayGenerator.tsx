import { useEffect, useState } from 'react';
import axiosClient, { extractErrorMessage } from './api/axiosClient';
import { generateId } from './generateId';
import type { Product } from './types/product';
import { type PutawayItemRow, type CreatePutawayPayload, type CreatedPutawayTask } from './types/putaway';

function randomContainerId(): string {
    const letters = Array.from({ length: 4 }, () => String.fromCharCode(65 + Math.floor(Math.random() * 26))).join('');
    const digits = Array.from({ length: 5 }, () => Math.floor(Math.random() * 10)).join('');
    return `${letters}${digits}`;
}

function emptyRow(): PutawayItemRow {
    return { id: generateId(), productSku: '', expectedQuantity: 1 };
}

export default function PutawayGenerator() {
    const [products, setProducts] = useState<Product[]>([]);
    const [loadingProducts, setLoadingProducts] = useState<boolean>(true);
    const [dataError, setDataError] = useState<string>('');

    // Filters the product picker in every item row by Name or SKU — purely a
    // client-side convenience, not sent to the backend.
    const [productSearch, setProductSearch] = useState<string>('');

    const [containerId, setContainerId] = useState<string>('');
    const [assignedSector, setAssignedSector] = useState<string>('');
    const [itemRows, setItemRows] = useState<PutawayItemRow[]>([emptyRow()]);

    const [submitting, setSubmitting] = useState<boolean>(false);
    const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

    useEffect(() => {
        void loadReferenceData();
    }, []);

    const loadReferenceData = async () => {
        setLoadingProducts(true);
        setDataError('');
        try {
            const productsResponse = await axiosClient.get<Product[]>('/Products/for-ordering');
            setProducts(productsResponse.data);
        } catch (error) {
            console.error('Failed to load products:', error);
            setDataError('Failed to load products. Is the backend reachable?');
        } finally {
            setLoadingProducts(false);
        }
    };

    const trimmedSector = assignedSector.trim();
    const trimmedSearch = productSearch.trim().toLowerCase();
    const filteredProducts = trimmedSearch
        ? products.filter(p =>
            p.name.toLowerCase().includes(trimmedSearch) || p.sku.toLowerCase().includes(trimmedSearch))
        : products;

    const handleAddItem = () => {
        setItemRows(prev => [...prev, emptyRow()]);
    };

    const handleRemoveItem = (id: string) => {
        setItemRows(prev => prev.filter(row => row.id !== id));
    };

    const updateRow = (id: string, patch: Partial<PutawayItemRow>) => {
        setItemRows(prev => prev.map(row => (row.id === id ? { ...row, ...patch } : row)));
    };

    const isValid =
        containerId.trim() !== '' &&
        trimmedSector !== '' &&
        itemRows.length > 0 &&
        itemRows.every(row => row.productSku !== '' && row.expectedQuantity >= 1);

    const resetForm = () => {
        setContainerId('');
        setAssignedSector('');
        setItemRows([emptyRow()]);
    };

    const handleSubmit = async () => {
        if (!isValid || submitting) return;

        setSubmitting(true);
        setMessage(null);
        try {
            const payload: CreatePutawayPayload = {
                containerBarcode: containerId.trim(),
                sector: trimmedSector,
                items: itemRows.map(row => ({
                    productSku: row.productSku,
                    expectedQuantity: row.expectedQuantity,
                })),
            };

            const response = await axiosClient.post<CreatedPutawayTask>('/PutawayTask', payload);

            setMessage({ type: 'success', text: `Putaway container ${response.data.containerBarcode} generated successfully.` });
            resetForm();
        } catch (error) {
            setMessage({ type: 'error', text: extractErrorMessage(error, 'Failed to generate the putaway container.') });
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="layout">
            <section className="panel">
                <h2>Putaway container</h2>

                {dataError && <div className="error-banner">{dataError}</div>}

                {message && (
                    <div className={message.type === 'success' ? 'success-banner' : 'error-banner'}>
                        {message.text}
                    </div>
                )}

                <div className="form-group">
                    <label htmlFor="containerId">Container ID</label>
                    <div className="inline-field">
                        <input
                            id="containerId"
                            type="text"
                            value={containerId}
                            onChange={(e) => setContainerId(e.target.value.trim())}
                            placeholder="e.g. HSOD51205"
                        />
                        <button type="button" className="secondary-btn" onClick={() => setContainerId(randomContainerId())}>
                            Random
                        </button>
                    </div>
                </div>

                <div className="form-group">
                    <label htmlFor="assignedSector">Assigned sector</label>
                    <input
                        id="assignedSector"
                        type="text"
                        value={assignedSector}
                        onChange={(e) => setAssignedSector(e.target.value)}
                        placeholder="e.g. mp1, mr1"
                    />
                </div>

                <button
                    className="primary-btn"
                    onClick={() => void handleSubmit()}
                    disabled={!isValid || submitting || loadingProducts}
                >
                    {submitting ? 'Generating...' : 'Generate putaway container'}
                </button>
            </section>

            <section className="panel">
                <div className="panel-header">
                    <h2>Expected items ({itemRows.length})</h2>
                    <button className="secondary-btn" onClick={handleAddItem}>+ Add Item</button>
                </div>

                {loadingProducts && <p className="muted">Loading products...</p>}

                <div className="form-group">
                    <label htmlFor="productSearch">Filter products</label>
                    <input
                        id="productSearch"
                        type="text"
                        value={productSearch}
                        onChange={(e) => setProductSearch(e.target.value)}
                        placeholder="Search by name or SKU..."
                    />
                </div>

                <div className="putaway-item-list">
                    {itemRows.map((row, index) => (
                        <div key={row.id} className="putaway-item-row">
                            <div className="putaway-item-row-header">
                                <span className="muted">Item {index + 1}</span>
                                <button
                                    type="button"
                                    className="remove-btn"
                                    onClick={() => handleRemoveItem(row.id)}
                                    disabled={itemRows.length === 1}
                                    title={itemRows.length === 1 ? 'At least one item is required' : 'Remove item'}
                                >
                                    ✕
                                </button>
                            </div>

                            <div className="form-group">
                                <label>Item SKU</label>
                                <select
                                    value={row.productSku}
                                    onChange={(e) => updateRow(row.id, { productSku: e.target.value })}
                                >
                                    <option value="">Select a product...</option>
                                    {filteredProducts.map(p => (
                                        <option key={p.id} value={p.sku}>
                                            {p.name} ({p.sku})
                                        </option>
                                    ))}
                                </select>
                                {trimmedSearch && filteredProducts.length === 0 && (
                                    <span className="field-error">No products match "{productSearch}".</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label>Expected quantity</label>
                                <input
                                    type="number"
                                    min={1}
                                    value={row.expectedQuantity}
                                    onChange={(e) => updateRow(row.id, { expectedQuantity: Math.max(1, Number(e.target.value)) })}
                                />
                            </div>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
}
