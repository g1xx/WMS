import { useState } from 'react';
import type { PutawayTask } from '../../types/putaway';

interface Props {
    task: PutawayTask;
    onConfirmItem: (locationBarcode: string, productSku: string, quantity: number) => Promise<void>;
    onReportMissing: (locationBarcode: string, productSku: string, missingQuantity: number) => Promise<void>;
}

export default function ActivePutawayScreen({ task, onConfirmItem, onReportMissing }: Props) {
    const [step, setStep] = useState<number>(1);
    const [localError, setLocalError] = useState<string>('');

    const [scanLocation, setScanLocation] = useState<string>('');
    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    const currentItem = task.items.find(i => i.putAwayQuantity + i.missingQuantity < i.expectedQuantity);
    const remaining = currentItem ? currentItem.expectedQuantity - currentItem.putAwayQuantity - currentItem.missingQuantity : 0;

    const handleLocationNext = () => {
        if (scanLocation.trim() === currentItem?.locationBarcode?.trim()) {
            setStep(2);
            setLocalError('');
        } else {
            setLocalError(`Wrong location! Go to: ${currentItem?.locationBarcode}`);
        }
    };

    const handleSkuNext = () => {
        if (scanSku.trim() === currentItem?.productSku?.trim()) {
            setStep(3);
            setLocalError('');
            setScanQty(remaining);
        } else {
            setLocalError(`Wrong item! Expected: ${currentItem?.productSku}`);
        }
    };

    const handleConfirm = async () => {
        if (!currentItem) return;
        setIsSubmitting(true);
        try {
            await onConfirmItem(currentItem.locationBarcode, currentItem.productSku, scanQty);
            setStep(1);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setLocalError('');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleReportMissingClick = async () => {
        if (!currentItem) return;

        // Explicit double-confirmation, unlike picking's missing-item flow: the
        // button click is the first confirmation, this dialog is the second —
        // there is no supervisor badge step for putaway shortages.
        const confirmed = window.confirm('Are you sure the item is missing? (Yes/No)');
        if (!confirmed) return;

        setIsSubmitting(true);
        try {
            await onReportMissing(currentItem.locationBarcode, currentItem.productSku, remaining);
            setStep(1);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setLocalError('');
        } finally {
            setIsSubmitting(false);
        }
    };

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
                <p style={{ margin: '5px 0', fontSize: '1.4rem' }}><strong>Location:</strong> <span style={{ color: '#64b5f6' }}>{currentItem.locationBarcode}</span></p>
                <p style={{ margin: '5px 0', fontSize: '1.2rem' }}><strong>Product:</strong> {currentItem.productName}</p>
                <p style={{ margin: '5px 0', color: '#a0a0a0' }}>SKU: {currentItem.productSku}</p>
                <p style={{ margin: '10px 0 5px 0', fontSize: '1.3rem', color: '#ffeb3b' }}>
                    <strong>Put away: {remaining} pcs</strong>
                </p>
            </div>

            <div style={{ width: '100%', backgroundColor: '#2a2a2a', padding: '15px', borderRadius: '8px', boxSizing: 'border-box' }}>
                {localError && (
                    <div style={{ backgroundColor: '#ff5252', color: 'white', padding: '10px', borderRadius: '4px', marginBottom: '15px', fontWeight: 'bold', textAlign: 'center' }}>
                        {localError}
                    </div>
                )}

                {step === 1 && (
                    <>
                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan Location {currentItem.locationBarcode}</p>
                        <input
                            type="text"
                            autoFocus
                            placeholder="Location barcode..."
                            value={scanLocation}
                            onChange={(e) => setScanLocation(e.target.value.trim())}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
                        />
                        <button onClick={handleLocationNext} disabled={!scanLocation} style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: scanLocation ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanLocation ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Check location</button>
                    </>
                )}

                {step === 2 && (
                    <>
                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan Item SKU {currentItem.productSku}</p>
                        <input
                            type="text"
                            autoFocus
                            placeholder="Product SKU..."
                            value={scanSku}
                            onChange={(e) => setScanSku(e.target.value.trim())}
                            style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
                        />
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(1)} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Back</button>
                            <button onClick={handleSkuNext} disabled={!scanSku} style={{ flex: 2, padding: '15px', backgroundColor: scanSku ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanSku ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Check SKU</button>
                        </div>
                    </>
                )}

                {step === 3 && (
                    <>
                        <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Confirm Quantity</p>
                        <div style={{ display: 'flex', gap: '10px', marginBottom: '15px' }}>
                            <label style={{ alignSelf: 'center', fontWeight: 'bold', fontSize: '1.2rem' }}>Qty:</label>
                            <input type="number" min="1" max={remaining} value={scanQty} onChange={(e) => setScanQty(Number(e.target.value))} style={{ flex: 1, padding: '12px', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }} />
                        </div>
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(2)} disabled={isSubmitting} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: isSubmitting ? 'not-allowed' : 'pointer' }}>Back</button>
                            <button onClick={handleConfirm} disabled={isSubmitting} style={{ flex: 2, padding: '15px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '4px', cursor: isSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}>
                                {isSubmitting ? 'Confirming...' : 'Confirm'}
                            </button>
                        </div>
                    </>
                )}

                <button
                    onClick={handleReportMissingClick}
                    disabled={isSubmitting}
                    style={{ width: '100%', padding: '12px', marginTop: '15px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '4px', cursor: isSubmitting ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
                >
                    ❌ Report Missing
                </button>
            </div>
        </div>
    );
}
