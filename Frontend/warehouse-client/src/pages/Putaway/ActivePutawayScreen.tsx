import { useState } from 'react';
import type { PutawayTask } from '../../types/putaway';
import { useSupervisorOverride } from '../../hooks/useSupervisorOverride';
import { usePutawayWizardSteps } from './usePutawayWizardSteps';

interface Props {
    task: PutawayTask;
    onConfirmItem: (locationBarcode: string, productSku: string, quantity: number) => Promise<void>;
    onReportMissing: (quantity: number, supervisorBadge: string) => Promise<void>;
}

export default function ActivePutawayScreen({ task, onConfirmItem, onReportMissing }: Props) {
    const {
        currentItem,
        remaining,
        suggestedLocations,
        locationInput, setLocationInput,
        scannedLocation,
        scanSku, setScanSku,
        scanQty, setScanQty,
        localError,
        isSubmitting,
        resetToLocationStep,
        confirmLocation,
        changeLocation,
        confirmItem,
    } = usePutawayWizardSteps({ task, onConfirmItem });

    // Supervisor-gated shortage submenu, shared with ActiveTaskScreen's missing-item flow.
    const missingOverride = useSupervisorOverride();
    const [missingQty, setMissingQty] = useState<number>(1);

    const handleReportMissingClick = () => {
        if (!currentItem) return;
        missingOverride.open();
        // Prefill with whatever is still left to put away
        setMissingQty(remaining);
    };

    const handleMissingSubmit = () => missingOverride.submit(async (badge) => {
        await onReportMissing(missingQty, badge);
        resetToLocationStep();
    });

    if (!currentItem) {
        return (
            <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', textAlign: 'center' }}>
                <p style={{ color: '#aaa' }}>Finishing up...</p>
            </div>
        );
    }

    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)' }}>
            <h3 style={{ margin: '0 0 10px 0', color: '#ff9800' }}>Container: {task.containerBarcode}</h3>

            <div style={{ borderLeft: '4px solid #ffeb3b', paddingLeft: '10px', marginBottom: '15px', backgroundColor: '#2a2a2a', padding: '15px' }}>
                <p style={{ margin: '5px 0', fontSize: '1.2rem' }}><strong>Product:</strong> {currentItem.productName}</p>
                <p style={{ margin: '5px 0', color: '#a0a0a0' }}>SKU: {currentItem.productSku}</p>
                <p style={{ margin: '10px 0 5px 0', fontSize: '1.3rem', color: '#ffeb3b' }}>
                    <strong>Put away: {remaining} pcs</strong>
                </p>
                <div style={{ margin: '10px 0 0 0', color: '#a0a0a0', fontSize: '0.9rem' }}>
                    <strong>Suggested locations:</strong>
                    {suggestedLocations.length === 0 ? (
                        <p style={{ margin: '4px 0 0 0' }}>none yet — first time storing this product</p>
                    ) : (
                        <ul style={{ margin: '6px 0 0 0', paddingLeft: '18px' }}>
                            {suggestedLocations.map(loc => (
                                <li key={loc.locationBarcode} style={{ marginBottom: '3px' }}>
                                    <span style={{ color: '#64b5f6', fontWeight: 'bold' }}>{loc.locationBarcode}</span>
                                    {!loc.isInCurrentSector && <span style={{ color: '#ff9800' }}> (other sector)</span>}
                                    {' — '}
                                    {loc.currentQuantity > 0 ? `${loc.currentQuantity} here` : 'empty home slot'}
                                    {' · '}
                                    {loc.distinctSkuCount}/{loc.maxDistinctSkus ?? '∞'} SKUs
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            </div>

            <div style={{ width: '100%', backgroundColor: '#2a2a2a', padding: '15px', borderRadius: '8px', boxSizing: 'border-box' }}>
                {localError && (
                    <div style={{ backgroundColor: '#ff5252', color: 'white', padding: '10px', borderRadius: '4px', marginBottom: '15px', fontWeight: 'bold', textAlign: 'center' }}>
                        {localError}
                    </div>
                )}

                {missingOverride.isOpen ? (
                    // ================= SUBMENU: REPORT MISSING =================
                    <>
                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Quantity missing (pcs):</p>
                        <input
                            type="number"
                            min="1"
                            max={remaining}
                            value={missingQty}
                            onChange={(e) => setMissingQty(Number(e.target.value))}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }}
                        />

                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan the supervisor's badge:</p>
                        <input
                            type="text"
                            autoFocus
                            placeholder="Supervisor barcode..."
                            value={missingOverride.badge}
                            onChange={(e) => missingOverride.setBadge(e.target.value.trim())}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem', textAlign: 'center' }}
                        />

                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={missingOverride.close} disabled={missingOverride.isSubmitting} style={{ flex: 1, padding: '12px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: missingOverride.isSubmitting ? 'not-allowed' : 'pointer' }}>Cancel</button>
                            <button onClick={handleMissingSubmit} disabled={missingOverride.isSubmitting} style={{ flex: 2, padding: '12px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '4px', cursor: missingOverride.isSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}>
                                {missingOverride.isSubmitting ? 'Reporting...' : 'Confirm'}
                            </button>
                        </div>
                    </>
                ) : !scannedLocation ? (
                    // ================= STEP 1: LOCATION =================
                    <>
                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan a destination location</p>
                        <input
                            type="text"
                            autoFocus
                            placeholder="Location barcode..."
                            value={locationInput}
                            onChange={(e) => setLocationInput(e.target.value.trim())}
                            onKeyDown={(e) => { if (e.key === 'Enter') confirmLocation(); }}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
                        />
                        <button
                            onClick={confirmLocation}
                            disabled={!locationInput}
                            style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: locationInput ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: locationInput ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}
                        >
                            Confirm location
                        </button>
                    </>
                ) : (
                    // ================= STEP 2: PRODUCT & QUANTITY =================
                    <>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px', backgroundColor: '#1e1e1e', padding: '10px 12px', borderRadius: '4px' }}>
                            <span style={{ color: '#a0a0a0' }}>
                                Location: <strong style={{ color: '#64b5f6', fontSize: '1.1rem' }}>{scannedLocation}</strong>
                            </span>
                            <button
                                onClick={changeLocation}
                                disabled={isSubmitting}
                                style={{ padding: '6px 10px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: isSubmitting ? 'not-allowed' : 'pointer', fontSize: '0.85rem' }}
                            >
                                Change Location
                            </button>
                        </div>

                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan Item SKU {currentItem.productSku}</p>
                        <input
                            type="text"
                            autoFocus
                            placeholder="Product SKU..."
                            value={scanSku}
                            onChange={(e) => setScanSku(e.target.value.trim())}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
                        />

                        <div style={{ display: 'flex', gap: '10px', marginBottom: '15px' }}>
                            <label style={{ alignSelf: 'center', fontWeight: 'bold', fontSize: '1.2rem' }}>Qty:</label>
                            <input
                                type="number"
                                min="1"
                                max={remaining}
                                value={scanQty}
                                onChange={(e) => setScanQty(Number(e.target.value))}
                                style={{ flex: 1, padding: '12px', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }}
                            />
                        </div>

                        <button
                            onClick={confirmItem}
                            disabled={isSubmitting || !scanSku}
                            style={{ width: '100%', padding: '15px', backgroundColor: (!isSubmitting && scanSku) ? '#4CAF50' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: (isSubmitting || !scanSku) ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                        >
                            {isSubmitting ? 'Confirming...' : 'Confirm putaway'}
                        </button>
                    </>
                )}

                {!missingOverride.isOpen && (
                    <button
                        onClick={handleReportMissingClick}
                        disabled={isSubmitting}
                        style={{ width: '100%', padding: '12px', marginTop: '15px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '4px', cursor: isSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                    >
                        ❌ Report Missing
                    </button>
                )}
            </div>
        </div>
    );
}
