import { useCallback, useEffect, useState } from 'react';
import { extractErrorMessage } from '../../api/axiosClient';
import { fetchLocationContents, fetchRelocationState, putAwayStock, takeStock } from '../../api/relocationApi';
import type { LocationContents, RelocationState, RelocationStockLine } from '../../types/relocation';

interface Props {
    onExitToMenu: () => void;
}

// Taking: scan a source location, scan (or pick from a list) a product, confirm a quantity.
// Putting away: for each carried SKU, scan a target location and confirm a quantity,
// repeating on the same SKU until it's fully placed before moving to the next.
type Mode = 'TAKE' | 'PUTAWAY';
type TakeStep = 'LOCATION' | 'PRODUCT' | 'QUANTITY';
type PutawayStep = 'LOCATION' | 'QUANTITY';

const panel: React.CSSProperties = {
    backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%',
    maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', position: 'relative',
};
const input: React.CSSProperties = {
    width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '10px',
    borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white',
};
const primaryButton: React.CSSProperties = {
    width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: '#2196F3',
    color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold',
};

export default function RelocationFlow({ onExitToMenu }: Props) {
    const [state, setState] = useState<RelocationState | null>(null);
    const [mode, setMode] = useState<Mode>('TAKE');
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    const [takeStep, setTakeStep] = useState<TakeStep>('LOCATION');
    const [sourceBarcode, setSourceBarcode] = useState('');
    const [sourceInput, setSourceInput] = useState('');
    const [productInput, setProductInput] = useState('');
    const [contents, setContents] = useState<LocationContents | null>(null);
    const [selectedLine, setSelectedLine] = useState<RelocationStockLine | null>(null);

    const [putawayStep, setPutawayStep] = useState<PutawayStep>('LOCATION');
    const [targetInput, setTargetInput] = useState('');

    const [quantity, setQuantity] = useState(1);
    const [busy, setBusy] = useState(false);

    // The SKU being placed: always the first still-carried line, so a split placement
    // keeps returning to the same SKU until it's fully placed.
    const currentCarried = state?.carriedItems[0] ?? null;

    const refresh = useCallback(async () => {
        try {
            setState(await fetchRelocationState());
        } catch (error) {
            alert(extractErrorMessage(error, 'Failed to load relocation state.'));
        }
    }, []);

    useEffect(() => { void refresh(); }, [refresh]);

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') setIsMenuOpen(prev => !prev);
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, []);

    const handleExit = () => {
        // Belt and braces — the exit button is already hidden while carrying, but the
        // rule is what matters, not the button.
        if (state && !state.canExit) {
            alert('You are still carrying stock. Put it away before leaving relocation.');
            return;
        }
        onExitToMenu();
    };

    // Step 1 -> 2. An empty product scan at step 2 lists what's here instead.
    const handleSourceNext = async () => {
        const barcode = sourceInput.trim();
        if (!barcode) return;
        setBusy(true);
        try {
            const loaded = await fetchLocationContents(barcode);
            setSourceBarcode(loaded.locationBarcode);
            setContents(loaded);
            setProductInput('');
            setSelectedLine(null);
            setTakeStep('PRODUCT');
        } catch (error) {
            alert(extractErrorMessage(error, 'Location not found.'));
        } finally {
            setBusy(false);
        }
    };

    const selectLine = (line: RelocationStockLine) => {
        setSelectedLine(line);
        // Defaults to what may actually be moved — physical minus reserved — never the
        // full physical quantity: reserved units belong to a pick task.
        setQuantity(line.availableQuantity);
        setTakeStep('QUANTITY');
    };

    // Enter with no input lists everything here; otherwise it's treated as a scan.
    const handleProductNext = () => {
        const sku = productInput.trim();
        if (!sku) return; // the list is already on screen — the worker picks from it
        const line = contents?.items.find(i => i.productSku.toLowerCase() === sku.toLowerCase());
        if (!line) {
            alert(`${sku} is not stocked at ${sourceBarcode}.`);
            return;
        }
        selectLine(line);
    };

    const handleTakeConfirm = async () => {
        if (!selectedLine) return;
        setBusy(true);
        try {
            setState(await takeStock(sourceBarcode, selectedLine.productSku, quantity));
            // Straight back to the product step at the same location: taking several SKUs
            // from one shelf is the common case, and re-scanning the shelf each time is noise.
            const reloaded = await fetchLocationContents(sourceBarcode);
            setContents(reloaded);
            setSelectedLine(null);
            setProductInput('');
            setTakeStep('PRODUCT');
        } catch (error) {
            alert(extractErrorMessage(error, 'Failed to take stock.'));
        } finally {
            setBusy(false);
        }
    };

    const handlePutawayLocationNext = () => {
        if (!targetInput.trim() || !currentCarried) return;
        setQuantity(currentCarried.availableQuantity);
        setPutawayStep('QUANTITY');
    };

    const handlePutawayConfirm = async () => {
        if (!currentCarried) return;
        setBusy(true);
        try {
            const next = await putAwayStock(targetInput.trim(), currentCarried.productSku, quantity);
            setState(next);
            setTargetInput('');
            setPutawayStep('LOCATION');

            if (next.carriedItems.length === 0) {
                alert('Relocation complete.');
                setMode('TAKE');
                setTakeStep('LOCATION');
                setSourceInput('');
                setContents(null);
            }
        } catch (error) {
            alert(extractErrorMessage(error, 'Failed to put stock away.'));
        } finally {
            setBusy(false);
        }
    };

    if (!state) return <p style={{ color: '#aaa' }}>Loading relocation...</p>;

    const carriedSummary = (
        <div style={{ backgroundColor: '#2a2a2a', borderRadius: '6px', padding: '10px', marginBottom: '15px' }}>
            <strong style={{ color: '#64b5f6', fontSize: '0.9rem' }}>Carrying</strong>
            {state.carriedItems.length === 0
                ? <p style={{ margin: '6px 0 0 0', color: '#888', fontSize: '0.85rem' }}>nothing</p>
                : state.carriedItems.map(line => (
                    <p key={line.productSku} style={{ margin: '6px 0 0 0', fontSize: '0.9rem' }}>
                        {line.productSku} — <strong style={{ color: '#ffeb3b' }}>{line.availableQuantity} pcs.</strong>
                    </p>
                ))}
        </div>
    );

    return (
        <div style={panel}>
            <h3 style={{ margin: '0 0 12px 0', color: '#9c27b0', paddingRight: '90px' }}>Relokacja</h3>
            <button
                onClick={() => setIsMenuOpen(true)}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Menu)
            </button>

            {carriedSummary}

            {mode === 'TAKE' && takeStep === 'LOCATION' && (
                <>
                    <p style={{ fontSize: '0.9rem', color: '#aaa' }}>Scan the location to take stock from.</p>
                    <input
                        autoFocus style={input} placeholder="Scan source location"
                        value={sourceInput} onChange={e => setSourceInput(e.target.value)}
                        onKeyDown={e => { if (e.key === 'Enter') void handleSourceNext(); }}
                    />
                    <button style={primaryButton} disabled={busy} onClick={() => void handleSourceNext()}>Check location</button>
                </>
            )}

            {mode === 'TAKE' && takeStep === 'PRODUCT' && (
                <>
                    <p style={{ fontSize: '0.9rem', color: '#aaa' }}>
                        At <strong style={{ color: '#64b5f6' }}>{sourceBarcode}</strong>. Scan a product, or press Enter to pick from the list.
                    </p>
                    <input
                        autoFocus style={input} placeholder="Scan product SKU (or Enter to list)"
                        value={productInput} onChange={e => setProductInput(e.target.value)}
                        onKeyDown={e => { if (e.key === 'Enter') handleProductNext(); }}
                    />
                    {contents?.items.length === 0 && <p style={{ color: '#888', fontSize: '0.85rem' }}>Nothing is stocked here.</p>}
                    {contents?.items.map(line => (
                        <button
                            key={line.productSku} onClick={() => selectLine(line)}
                            disabled={line.availableQuantity <= 0}
                            style={{
                                width: '100%', textAlign: 'left', padding: '10px', marginBottom: '8px',
                                backgroundColor: '#2a2a2a', color: line.availableQuantity > 0 ? '#e0e0e0' : '#777',
                                border: '1px solid #444', borderRadius: '6px',
                                cursor: line.availableQuantity > 0 ? 'pointer' : 'not-allowed',
                            }}
                        >
                            <div><strong>{line.productSku}</strong> — {line.productName}</div>
                            <div style={{ fontSize: '0.8rem', color: '#aaa' }}>
                                {line.availableQuantity} movable
                                {line.reservedQuantity > 0 && ` · ${line.reservedQuantity} reserved for picking`}
                            </div>
                        </button>
                    ))}
                    <button
                        style={{ ...primaryButton, backgroundColor: '#555', marginTop: '6px' }}
                        onClick={() => { setTakeStep('LOCATION'); setSourceInput(''); setContents(null); }}
                    >
                        Another location
                    </button>
                </>
            )}

            {mode === 'TAKE' && takeStep === 'QUANTITY' && selectedLine && (
                <>
                    <p style={{ fontSize: '0.9rem', color: '#aaa' }}>
                        Taking <strong>{selectedLine.productSku}</strong> from {sourceBarcode}.
                        {selectedLine.reservedQuantity > 0 && (
                            <> {selectedLine.reservedQuantity} of {selectedLine.physicalQuantity} are reserved for a pick task and cannot be moved.</>
                        )}
                    </p>
                    <input
                        autoFocus type="number" style={input} min={1} max={selectedLine.availableQuantity}
                        value={quantity} onChange={e => setQuantity(Number(e.target.value))}
                        onKeyDown={e => { if (e.key === 'Enter') void handleTakeConfirm(); }}
                    />
                    <button style={primaryButton} disabled={busy} onClick={() => void handleTakeConfirm()}>Confirm</button>
                    <button
                        style={{ ...primaryButton, backgroundColor: '#555', marginTop: '8px' }}
                        onClick={() => setTakeStep('PRODUCT')}
                    >
                        Back
                    </button>
                </>
            )}

            {mode === 'PUTAWAY' && currentCarried && putawayStep === 'LOCATION' && (
                <>
                    <p style={{ fontSize: '0.9rem', color: '#aaa' }}>
                        Where does <strong style={{ color: '#ffeb3b' }}>{currentCarried.productSku}</strong> ({currentCarried.availableQuantity} left) go?
                    </p>
                    <input
                        autoFocus style={input} placeholder="Scan target location"
                        value={targetInput} onChange={e => setTargetInput(e.target.value)}
                        onKeyDown={e => { if (e.key === 'Enter') handlePutawayLocationNext(); }}
                    />
                    <button style={primaryButton} onClick={handlePutawayLocationNext}>Next</button>
                </>
            )}

            {mode === 'PUTAWAY' && currentCarried && putawayStep === 'QUANTITY' && (
                <>
                    <p style={{ fontSize: '0.9rem', color: '#aaa' }}>
                        Placing <strong>{currentCarried.productSku}</strong> into {targetInput}. Enter less than {currentCarried.availableQuantity} to split across locations.
                    </p>
                    <input
                        autoFocus type="number" style={input} min={1} max={currentCarried.availableQuantity}
                        value={quantity} onChange={e => setQuantity(Number(e.target.value))}
                        onKeyDown={e => { if (e.key === 'Enter') void handlePutawayConfirm(); }}
                    />
                    <button style={primaryButton} disabled={busy} onClick={() => void handlePutawayConfirm()}>Confirm</button>
                    <button
                        style={{ ...primaryButton, backgroundColor: '#555', marginTop: '8px' }}
                        onClick={() => setPutawayStep('LOCATION')}
                    >
                        Back
                    </button>
                </>
            )}

            {mode === 'PUTAWAY' && !currentCarried && (
                <p style={{ color: '#888' }}>Nothing left to put away.</p>
            )}

            {isMenuOpen && (
                <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', backgroundColor: 'rgba(0,0,0,0.92)', borderRadius: '8px', display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '20px', boxSizing: 'border-box', zIndex: 10 }}>
                    <h3 style={{ color: '#9c27b0', textAlign: 'center', marginTop: 0 }}>Relocation menu</h3>

                    <button
                        style={{ ...primaryButton, marginBottom: '12px' }}
                        onClick={() => { setMode('TAKE'); setTakeStep('LOCATION'); setSourceInput(''); setContents(null); setIsMenuOpen(false); }}
                    >
                        Take from another location
                    </button>

                    <button
                        style={{ ...primaryButton, backgroundColor: '#ff9800', marginBottom: '12px' }}
                        disabled={state.carriedItems.length === 0}
                        onClick={() => { setMode('PUTAWAY'); setPutawayStep('LOCATION'); setTargetInput(''); setIsMenuOpen(false); }}
                    >
                        Start putting away {state.carriedItems.length === 0 && '(nothing carried)'}
                    </button>

                    {/* Hidden rather than disabled while carrying: the reason it's unavailable
                        is worth stating, and a dead button doesn't state it. */}
                    {state.canExit ? (
                        <button style={{ ...primaryButton, backgroundColor: '#f44336', marginBottom: '12px' }} onClick={handleExit}>
                            Exit relocation
                        </button>
                    ) : (
                        <p style={{ color: '#ff9800', fontSize: '0.85rem', textAlign: 'center' }}>
                            You are carrying stock — put it away before leaving.
                        </p>
                    )}

                    <button style={{ ...primaryButton, backgroundColor: '#555' }} onClick={() => setIsMenuOpen(false)}>
                        Close (Esc)
                    </button>
                </div>
            )}
        </div>
    );
}
