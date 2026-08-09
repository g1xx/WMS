import { useEffect, useMemo, useState } from 'react';
import axiosClient from './api/axiosClient';
import { type Product, getAvailableQuantity } from './types/product';
import type { CreatedOrder, LogEntry, LogStatus, OrderCreatePayload, OrderMode } from './types/order';

const RANDOM_MAX_LINES = 5;

function randomInt(minInclusive: number, maxInclusive: number): number {
    return minInclusive + Math.floor(Math.random() * (maxInclusive - minInclusive + 1));
}

function extractErrorMessage(error: unknown, fallback: string): string {
    const data = (error as { response?: { data?: unknown } })?.response?.data;
    if (typeof data === 'string' && data.trim()) return data;
    if (data && typeof data === 'object' && 'message' in data) {
        const message = (data as { message?: unknown }).message;
        if (typeof message === 'string' && message.trim()) return message;
    }
    return fallback;
}

export default function PickingGenerator() {
    const [products, setProducts] = useState<Product[]>([]);
    const [loadingProducts, setLoadingProducts] = useState<boolean>(true);
    const [productsError, setProductsError] = useState<string>('');

    const [selections, setSelections] = useState<Record<string, number>>({});
    const [quantityErrors, setQuantityErrors] = useState<Record<string, string>>({});

    const [customerName, setCustomerName] = useState<string>('');
    const [destinationAddress, setDestinationAddress] = useState<string>('');

    const [submitting, setSubmitting] = useState<boolean>(false);
    const [log, setLog] = useState<LogEntry[]>([]);

    useEffect(() => {
        void loadProducts();
    }, []);

    const loadProducts = async () => {
        setLoadingProducts(true);
        setProductsError('');
        try {
            const response = await axiosClient.get<Product[]>('/Products');
            setProducts(response.data);
        } catch (error) {
            console.error('Failed to load products:', error);
            setProductsError('Failed to load products. Is the backend running on http://localhost:5124?');
        } finally {
            setLoadingProducts(false);
        }
    };

    const appendLog = (entry: Omit<LogEntry, 'id'>) => {
        setLog(prev => [{ id: crypto.randomUUID(), ...entry }, ...prev]);
    };

    const submitOrder = async (payload: OrderCreatePayload, mode: OrderMode) => {
        setSubmitting(true);
        try {
            const createResponse = await axiosClient.post<CreatedOrder>('/Orders', payload);
            const order = createResponse.data;
            appendLog({ mode, orderNumber: order.orderNumber, status: 'created', message: `Order ${order.orderNumber} created with ${payload.items.length} line(s).` });

            try {
                const allocateResponse = await axiosClient.post(`/Orders/${order.id}/allocate`);
                appendLog({ mode, orderNumber: order.orderNumber, status: 'allocated', message: extractErrorMessage({ response: allocateResponse }, 'Order allocated.') });
            } catch (allocateError) {
                appendLog({
                    mode,
                    orderNumber: order.orderNumber,
                    status: 'shortage' as LogStatus,
                    message: extractErrorMessage(allocateError, 'Allocation failed for an unknown reason.'),
                });
            }

            if (mode === 'manual') {
                setSelections({});
            }

            // Stock reservations changed; refresh so AvailableQuantity stays accurate
            await loadProducts();
        } catch (createError) {
            appendLog({ mode, status: 'error', message: extractErrorMessage(createError, 'Failed to create the order.') });
        } finally {
            setSubmitting(false);
        }
    };

    const handleQuantityChange = (product: Product, rawValue: string) => {
        const available = getAvailableQuantity(product);

        if (rawValue === '') {
            setSelections(prev => {
                const next = { ...prev };
                delete next[product.id];
                return next;
            });
            setQuantityErrors(prev => {
                const next = { ...prev };
                delete next[product.id];
                return next;
            });
            return;
        }

        const parsed = Number(rawValue);
        if (!Number.isFinite(parsed) || parsed < 0) return;

        // Block: an over-limit quantity never reaches state, so the input snaps back
        if (parsed > available) {
            setQuantityErrors(prev => ({ ...prev, [product.id]: `Only ${available} available - quantity blocked.` }));
            return;
        }

        setQuantityErrors(prev => {
            const next = { ...prev };
            delete next[product.id];
            return next;
        });
        setSelections(prev => ({ ...prev, [product.id]: parsed }));
    };

    const selectedItems = useMemo(
        () => Object.entries(selections).filter(([, qty]) => qty > 0),
        [selections]
    );

    const isManualValid = useMemo(() => {
        if (!customerName.trim() || !destinationAddress.trim()) return false;
        if (selectedItems.length === 0) return false;
        if (Object.keys(quantityErrors).length > 0) return false;

        return selectedItems.every(([productId, qty]) => {
            const product = products.find(p => p.id === productId);
            return product != null && qty >= 1 && qty <= getAvailableQuantity(product);
        });
    }, [customerName, destinationAddress, selectedItems, quantityErrors, products]);

    const handleManualSubmit = async () => {
        if (!isManualValid || submitting) return;

        const items = selectedItems.map(([productId, requiredQuantity]) => ({ productId, requiredQuantity }));
        await submitOrder(
            { customerName: customerName.trim(), destinationAddress: destinationAddress.trim(), items },
            'manual'
        );
    };

    const handleGenerateRandom = async () => {
        if (submitting) return;

        // a) drop everything with no available stock
        const inStock = products.filter(p => getAvailableQuantity(p) > 0);
        if (inStock.length === 0) {
            alert('No products currently have available stock to generate an order from.');
            return;
        }

        // b) pick a random subset of the remaining products
        const subsetSize = randomInt(1, Math.min(RANDOM_MAX_LINES, inStock.length));
        const shuffled = [...inStock].sort(() => Math.random() - 0.5);
        const chosen = shuffled.slice(0, subsetSize);

        // c) a random quantity between 1 and AvailableQuantity for each chosen product
        const newSelections: Record<string, number> = {};
        const items = chosen.map(product => {
            const available = getAvailableQuantity(product);
            const quantity = randomInt(1, available);
            newSelections[product.id] = quantity;
            return { productId: product.id, requiredQuantity: quantity };
        });

        const finalCustomerName = customerName.trim() || `Test Customer ${randomInt(1000, 9999)}`;
        const finalDestination = destinationAddress.trim() || '1 Test Warehouse Ave, Random City';

        setSelections(newSelections);
        setQuantityErrors({});
        setCustomerName(finalCustomerName);
        setDestinationAddress(finalDestination);

        // d) submit the order - by construction every line is within AvailableQuantity,
        // so this is guaranteed to clear allocation without hitting AwaitingReplenishment
        await submitOrder(
            { customerName: finalCustomerName, destinationAddress: finalDestination, items },
            'random'
        );
    };

    return (
        <div className="layout">
            <section className="panel">
                <div className="panel-header">
                    <h2>Products</h2>
                    <button className="secondary-btn" onClick={() => void loadProducts()} disabled={loadingProducts}>
                        {loadingProducts ? 'Loading...' : 'Refresh'}
                    </button>
                </div>

                {productsError && <div className="error-banner">{productsError}</div>}

                {!productsError && products.length === 0 && !loadingProducts && (
                    <p className="muted">No products found.</p>
                )}

                <div className="product-list">
                    {products.map(product => {
                        const available = getAvailableQuantity(product);
                        const isOutOfStock = available === 0;
                        const quantity = selections[product.id] ?? '';
                        const error = quantityErrors[product.id];

                        return (
                            <div key={product.id} className={`product-row ${isOutOfStock ? 'out-of-stock' : ''}`}>
                                <div className="product-info">
                                    <strong>{product.name}</strong>
                                    <span className="muted">SKU: {product.sku}</span>
                                </div>
                                <div className="product-stock">
                                    Available: <strong>{available}</strong>
                                </div>
                                <div className="product-qty">
                                    <input
                                        type="number"
                                        min={0}
                                        max={available}
                                        placeholder="0"
                                        value={quantity}
                                        disabled={isOutOfStock || submitting}
                                        onChange={(e) => handleQuantityChange(product, e.target.value)}
                                    />
                                </div>
                                {error && <div className="field-error">{error}</div>}
                            </div>
                        );
                    })}
                </div>
            </section>

            <section className="panel">
                <h2>Order details</h2>
                <div className="form-group">
                    <label htmlFor="customerName">Customer name</label>
                    <input
                        id="customerName"
                        type="text"
                        value={customerName}
                        onChange={(e) => setCustomerName(e.target.value)}
                        placeholder="Test Customer"
                    />
                </div>
                <div className="form-group">
                    <label htmlFor="destinationAddress">Destination address</label>
                    <input
                        id="destinationAddress"
                        type="text"
                        value={destinationAddress}
                        onChange={(e) => setDestinationAddress(e.target.value)}
                        placeholder="1 Test Warehouse Ave"
                    />
                </div>

                <div className="actions">
                    <button
                        className="primary-btn"
                        onClick={() => void handleManualSubmit()}
                        disabled={!isManualValid || submitting}
                    >
                        {submitting ? 'Submitting...' : `Submit manual order (${selectedItems.length} line${selectedItems.length === 1 ? '' : 's'})`}
                    </button>
                    <button
                        className="random-btn"
                        onClick={() => void handleGenerateRandom()}
                        disabled={submitting || loadingProducts}
                    >
                        Generate random order
                    </button>
                </div>

                <div className="log-header">
                    <h2>Activity log</h2>
                    {log.length > 0 && (
                        <button className="secondary-btn" onClick={() => setLog([])}>Clear</button>
                    )}
                </div>
                <div className="log-list">
                    {log.length === 0 && <p className="muted">No orders submitted yet.</p>}
                    {log.map(entry => (
                        <div key={entry.id} className={`log-entry log-${entry.status}`}>
                            <div className="log-entry-header">
                                <span className="log-mode">{entry.mode === 'manual' ? 'Manual' : 'Random'}</span>
                                {entry.orderNumber && <span className="log-order">{entry.orderNumber}</span>}
                                <span className={`log-badge log-badge-${entry.status}`}>{entry.status}</span>
                            </div>
                            <div className="log-message">{entry.message}</div>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
}
