import { useEffect, useState } from 'react';
import { extractErrorMessage } from '../../api/axiosClient';
import { fetchContainerInfo, fetchLocationInfo, fetchProductInfo } from '../../api/infoApi';
import type { ContainerContentSection, ContainerInfo, LocationInfo, ProductInfo } from '../../types/info';

interface Props {
    onExitToMenu: () => void;
}

type Mode = 'TOWAR' | 'POJEMNIK' | 'MIEJSCE';

const MODE_LABELS: Record<Mode, string> = {
    TOWAR: 'Towar',
    POJEMNIK: 'Pojemnik',
    MIEJSCE: 'Miejsce',
};

const MODE_PROMPTS: Record<Mode, string> = {
    // SKU, not a barcode: products have no barcode in this system.
    TOWAR: 'Scan or type a product SKU',
    POJEMNIK: 'Scan a container barcode',
    MIEJSCE: 'Scan a location barcode',
};

const panel: React.CSSProperties = {
    backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%',
    maxWidth: '420px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', position: 'relative',
};
const inputStyle: React.CSSProperties = {
    width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '10px',
    borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white',
};
const card: React.CSSProperties = {
    backgroundColor: '#2a2a2a', borderRadius: '6px', padding: '12px', marginBottom: '10px',
};

function Row({ label, value }: { label: string; value: React.ReactNode }) {
    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: '10px', marginBottom: '4px', fontSize: '0.9rem' }}>
            <span style={{ color: '#888' }}>{label}</span>
            <span style={{ textAlign: 'right' }}>{value}</span>
        </div>
    );
}

// Quantities always read physical / reserved / available together — showing physical alone
// invites relocating or promising units a pick task has already claimed.
function Quantities({ physical, reserved, available }: { physical: number; reserved: number; available: number }) {
    return (
        <div style={{ fontSize: '0.8rem', color: '#aaa' }}>
            <span style={{ color: '#4CAF50', fontWeight: 'bold' }}>{available} available</span>
            {' · '}{physical} physical
            {reserved > 0 && <span style={{ color: '#ff9800' }}>{' · '}{reserved} reserved</span>}
        </div>
    );
}

// Container contents are derived from task lines, and the sections differ in how much they
// can be trusted. That difference has to be visible, not just worded: a worker glancing at
// the screen must not mistake a historical reconstruction for a live count.
//
// Live sections (BeingPickedInto, ToBePutAway) get the solid card the rest of the screen
// uses. AsDispatched gets a dashed amber frame, a muted body and an explicit caveat, so it
// reads as a record of something that happened rather than a statement of what is there.
function ContentSection({ section }: { section: ContainerContentSection }) {
    if (section.kind === 'Empty') {
        return (
            <div style={{ ...card, borderLeft: '4px solid #4CAF50' }}>
                <strong style={{ color: '#81c784', fontSize: '0.9rem' }}>Empty</strong>
                <p style={{ margin: '6px 0 0 0', color: '#aaa', fontSize: '0.85rem' }}>
                    Released back to the free pool, so its putaway finished.
                </p>
            </div>
        );
    }

    if (section.kind === 'Unknown') {
        // Never "empty": nothing has ever been recorded against this container, which is a
        // different statement from "there is nothing in it".
        return (
            <div style={{ ...card, borderLeft: '4px solid #ff9800' }}>
                <strong style={{ color: '#ffb74d', fontSize: '0.9rem' }}>Contents unknown</strong>
                <p style={{ margin: '6px 0 0 0', color: '#aaa', fontSize: '0.85rem' }}>
                    No task has recorded anything for this container. That is not the same as it being empty.
                </p>
            </div>
        );
    }

    const isHistory = section.isHistorical;
    const title = section.kind === 'BeingPickedInto' ? 'Being picked into'
        : section.kind === 'ToBePutAway' ? 'To be put away'
        : 'As dispatched';

    return (
        <div
            style={{
                ...card,
                backgroundColor: isHistory ? 'transparent' : '#2a2a2a',
                border: isHistory ? '1px dashed #8d6e63' : '1px solid transparent',
            }}
        >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                <strong style={{ color: isHistory ? '#a1887f' : '#64b5f6', fontSize: '0.9rem' }}>
                    {isHistory && '🕓 '}{title}
                </strong>
                {section.sector && <span style={{ color: '#888', fontSize: '0.75rem' }}>{section.sector}</span>}
            </div>

            {isHistory && (
                // The caveat that matters to someone standing in front of the container:
                // nothing ever invalidates this line.
                <p style={{ margin: '6px 0', color: '#a1887f', fontSize: '0.8rem', fontStyle: 'italic' }}>
                    What was picked into it at dispatch — not verified since. Only a completed putaway
                    clears this, so if it was emptied any other way, this keeps saying the same thing.
                </p>
            )}

            {section.lines.length === 0 ? (
                <p style={{ margin: '6px 0 0 0', color: '#888', fontSize: '0.85rem' }}>Nothing outstanding.</p>
            ) : (
                section.lines.map(line => (
                    <div key={line.productSku} style={{ marginTop: '6px', fontSize: '0.9rem', color: isHistory ? '#bcaaa4' : '#e0e0e0' }}>
                        <strong>{line.productSku}</strong> — {line.productName}
                        <span style={{ color: isHistory ? '#a1887f' : '#ffeb3b', fontWeight: 'bold' }}>
                            {' · '}{line.quantity} pcs
                        </span>
                    </div>
                ))
            )}

            {section.sourceTaskId && (
                <div style={{ marginTop: '8px', color: '#777', fontSize: '0.72rem' }}>
                    from task {section.sourceTaskId.substring(0, 8)}...
                </div>
            )}
        </div>
    );
}

export default function InfoScreen({ onExitToMenu }: Props) {
    const [mode, setMode] = useState<Mode>('TOWAR');
    const [query, setQuery] = useState('');
    const [busy, setBusy] = useState(false);

    const [product, setProduct] = useState<ProductInfo | null>(null);
    const [container, setContainer] = useState<ContainerInfo | null>(null);
    const [location, setLocation] = useState<LocationInfo | null>(null);

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') onExitToMenu();
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [onExitToMenu]);

    const clearResults = () => { setProduct(null); setContainer(null); setLocation(null); };

    const switchMode = (next: Mode) => {
        setMode(next);
        setQuery('');
        clearResults();
    };

    const lookup = async () => {
        const trimmed = query.trim();
        if (!trimmed) return;

        setBusy(true);
        clearResults();
        try {
            if (mode === 'TOWAR') setProduct(await fetchProductInfo(trimmed));
            else if (mode === 'POJEMNIK') setContainer(await fetchContainerInfo(trimmed));
            else setLocation(await fetchLocationInfo(trimmed));
        } catch (error) {
            alert(extractErrorMessage(error, 'Nothing found for that code.'));
        } finally {
            setBusy(false);
        }
    };

    return (
        <div style={panel}>
            <button
                onClick={onExitToMenu}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Menu)
            </button>

            <h3 style={{ margin: '0 0 12px 0', color: '#00bcd4', paddingRight: '90px' }}>Informacja o...</h3>

            <div style={{ display: 'flex', gap: '6px', marginBottom: '12px' }} role="tablist">
                {(Object.keys(MODE_LABELS) as Mode[]).map(m => (
                    <button
                        key={m}
                        role="tab"
                        aria-selected={mode === m}
                        onClick={() => switchMode(m)}
                        style={{
                            flex: 1, padding: '10px', border: 'none', borderRadius: '6px', cursor: 'pointer',
                            fontWeight: 'bold', backgroundColor: mode === m ? '#00bcd4' : '#333',
                            color: mode === m ? '#0b0b0b' : '#aaa',
                        }}
                    >
                        {MODE_LABELS[m]}
                    </button>
                ))}
            </div>

            <input
                autoFocus
                style={inputStyle}
                placeholder={MODE_PROMPTS[mode]}
                value={query}
                onChange={e => setQuery(e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') void lookup(); }}
            />
            <button
                onClick={() => void lookup()}
                disabled={busy || !query.trim()}
                style={{
                    width: '100%', padding: '14px', fontSize: '1.05rem', border: 'none', borderRadius: '4px',
                    fontWeight: 'bold', color: 'white',
                    backgroundColor: (busy || !query.trim()) ? '#555' : '#2196F3',
                    cursor: (busy || !query.trim()) ? 'not-allowed' : 'pointer',
                }}
            >
                {busy ? 'Searching...' : 'Look up'}
            </button>

            {product && (
                <div style={{ marginTop: '15px' }}>
                    <div style={card}>
                        <h4 style={{ margin: '0 0 8px 0', color: '#64b5f6' }}>{product.name}</h4>
                        <Row label="SKU" value={product.sku} />
                        <Row label="Weight" value={`${product.weightKg} kg`} />
                        <Row label="Dimensions" value={`${product.lengthCm} × ${product.widthCm} × ${product.heightCm} cm`} />
                        <Row label="Size" value={product.sizeCategory} />
                    </div>

                    <h4 style={{ margin: '12px 0 8px 0', color: '#64b5f6', fontSize: '0.95rem' }}>
                        Locations ({product.locations.length})
                    </h4>
                    {product.locations.length === 0 && (
                        <p style={{ color: '#888', fontSize: '0.85rem' }}>Never stored anywhere yet.</p>
                    )}
                    {product.locations.map(line => (
                        <div key={line.locationBarcode} style={card}>
                            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                <strong style={{ color: '#64b5f6' }}>{line.locationBarcode}</strong>
                                <span style={{ color: '#888', fontSize: '0.8rem' }}>{line.locationType}</span>
                            </div>
                            {line.physicalQuantity === 0 ? (
                                // Not noise: an empty row is where this SKU lives when stocked.
                                <div style={{ fontSize: '0.8rem', color: '#888' }}>empty — home slot</div>
                            ) : (
                                <Quantities
                                    physical={line.physicalQuantity}
                                    reserved={line.reservedQuantity}
                                    available={line.availableQuantity}
                                />
                            )}
                        </div>
                    ))}

                    {product.carriedByWorkersQuantity > 0 && (
                        // Transit stock is real but has no address anyone can walk to, so it
                        // sits outside the list as a total rather than being hidden.
                        <div style={{ ...card, borderLeft: '4px solid #9c27b0' }}>
                            <strong style={{ color: '#ce93d8' }}>{product.carriedByWorkersQuantity} pcs</strong>
                            <div style={{ fontSize: '0.8rem', color: '#aaa' }}>
                                currently carried by workers (in relocation) — no fixed location
                            </div>
                        </div>
                    )}
                </div>
            )}

            {container && (
                <div style={{ marginTop: '15px' }}>
                    <div style={card}>
                        <h4 style={{ margin: '0 0 8px 0', color: '#64b5f6' }}>{container.barcode}</h4>
                        <Row label="Type" value={container.type} />
                        <Row label="Status" value={container.status} />
                        <Row label="Location" value={container.locationBarcode ?? '—'} />
                        <Row label="Sector" value={container.assignedSector ?? '—'} />
                    </div>

                    <div style={card}>
                        <strong style={{ color: '#64b5f6', fontSize: '0.9rem' }}>
                            Linked tasks ({container.linkedTasks.length})
                        </strong>
                        {container.linkedTasks.length === 0 ? (
                            <p style={{ margin: '6px 0 0 0', color: '#888', fontSize: '0.85rem' }}>
                                Not held by any task.
                            </p>
                        ) : (
                            container.linkedTasks.map(task => (
                                <div key={task.taskId} style={{ marginTop: '8px', paddingTop: '8px', borderTop: '1px solid #3a3a3a' }}>
                                    <Row label="Flow" value={task.kind} />
                                    <Row label="Status" value={task.status} />
                                    <Row label="Sector" value={task.sector} />
                                    <Row label="Task" value={task.taskId.substring(0, 8) + '...'} />
                                </div>
                            ))
                        )}
                    </div>

                    {container.contentSections.map((section, index) => (
                        <ContentSection key={`${section.kind}-${section.sourceTaskId ?? index}`} section={section} />
                    ))}
                </div>
            )}

            {location && (
                <div style={{ marginTop: '15px' }}>
                    <div style={card}>
                        <h4 style={{ margin: '0 0 8px 0', color: '#64b5f6' }}>{location.barcode}</h4>
                        <Row label="Type" value={location.type} />
                        <Row label="Sector" value={location.sector || '—'} />
                        <Row label="Zone" value={location.zoneCode} />
                        <Row
                            label="Distinct SKUs"
                            value={`${location.distinctSkuCount} / ${location.maxDistinctSkus ?? '∞'}`}
                        />
                    </div>

                    <h4 style={{ margin: '12px 0 8px 0', color: '#64b5f6', fontSize: '0.95rem' }}>
                        Stored here ({location.items.length})
                    </h4>
                    {location.items.length === 0 && (
                        <p style={{ color: '#888', fontSize: '0.85rem' }}>Nothing stored here.</p>
                    )}
                    {location.items.map(item => (
                        <div key={item.productSku} style={card}>
                            <div><strong>{item.productSku}</strong> — {item.productName}</div>
                            <Quantities
                                physical={item.physicalQuantity}
                                reserved={item.reservedQuantity}
                                available={item.availableQuantity}
                            />
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
