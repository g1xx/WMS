import { useState, useEffect } from 'react';
import type { PickTask } from '../../types/task';

interface Props {
    task: PickTask;
    scanLocation: string;
    setScanLocation: (val: string) => void;
    scanSku: string;
    setScanSku: (val: string) => void;
    scanQty: number;
    setScanQty: (val: number) => void;
    onPickItem: () => Promise<void>;
    onDispatch: (containerBarcode: string, conveyorBarcode: string) => Promise<void>;
    onCancel: () => Promise<void>;
    onReportDefect: (locationBarcode: string, productSku: string, defectiveQuantity: number, supervisorBadge: string) => Promise<void>;
    onReportMissing: (locationBarcode: string, productSku: string, missingQuantity: number, supervisorBadge: string) => Promise<void>;
}

export default function ActiveTaskScreen({
    task, scanLocation, setScanLocation, scanSku, setScanSku, scanQty, setScanQty, onPickItem, onDispatch, onCancel, onReportDefect, onReportMissing
}: Props) {
    const [step, setStep] = useState<number>(1);
    const [localError, setLocalError] = useState<string>('');
    const [isMenuOpen, setIsMenuOpen] = useState<boolean>(false);

    const [isDispatchMode, setIsDispatchMode] = useState<boolean>(false);
    const [dispatchContainer, setDispatchContainer] = useState<string>('');
    const [dispatchConveyor, setDispatchConveyor] = useState<string>('');

    const [isOverviewOpen, setIsOverviewOpen] = useState<boolean>(false);

    const [isMissingMode, setIsMissingMode] = useState<boolean>(false);
    const [missingQty, setMissingQty] = useState<number>(1);
    // Shared between the "missing" and "defect" submenus: both are supervisor-gated
    // actions authorized the same way, and only one submenu is ever open at a time.
    const [supervisorBadge, setSupervisorBadge] = useState<string>('');
    const [isReportingMissing, setIsReportingMissing] = useState<boolean>(false);

    const [isDefectMode, setIsDefectMode] = useState<boolean>(false);
    const [defectiveQty, setDefectiveQty] = useState<number>(1);
    const [isReportingDefect, setIsReportingDefect] = useState<boolean>(false);

    const currentItem = task.items.find(item => item.pickedQuantity < item.requiredQuantity);
    const hasPickedItems = task.items.some(item => item.pickedQuantity > 0);

    // The exact container the worker must scan to close this task out.
    // Trimmed defensively: scanner input and the backend value may carry a trailing space.
    const expectedContainer = task.containerBarcode?.trim();

    useEffect(() => {
        if (!currentItem && !isMenuOpen) {
            setIsDispatchMode(true);
        }
    }, [currentItem, isMenuOpen]);

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && !isDispatchMode) {
                // Esc always returns to the main menu, resetting any submenu
                setIsMissingMode(false);
                setIsDefectMode(false);
                setIsMenuOpen(prev => !prev);
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isDispatchMode]);

    const handleLocationNext = () => {
        if (scanLocation.trim() === currentItem?.locationBarcode?.trim()) {
            setStep(2); setLocalError('');
        } else {
            setLocalError(`Wrong location! Go to: ${currentItem?.locationBarcode}`);
        }
    };

    const handleSkuNext = () => {
        if (scanSku.trim() === currentItem?.productSku?.trim()) {
            setStep(3); setLocalError('');
            setScanQty(currentItem.requiredQuantity - currentItem.pickedQuantity);
        } else {
            setLocalError(`Wrong product! Expected: ${currentItem?.productSku}`);
        }
    };

    const handleConfirm = async () => {
        await onPickItem();
        setStep(1);
        setLocalError('');
    };

    const handleDispatchSubmit = () => {
        const trimmedContainer = dispatchContainer.trim();
        const trimmedConveyor = dispatchConveyor.trim();

        if (!trimmedContainer || !trimmedConveyor) {
            alert("Please scan both the container and the conveyor!");
            return;
        }

        if (expectedContainer && trimmedContainer !== expectedContainer) {
            alert(`Error! Wrong container.\nExpected: ${expectedContainer}\nThis container is already assigned to another order!`);
            setDispatchContainer(''); // Clear the field to force a rescan of the correct one
            return;
        }

        // Send the trimmed values: a trailing space from the scanner must not reach the backend
        onDispatch(trimmedContainer, trimmedConveyor);
    };

    const handleMissingSubmit = async () => {
        if (!currentItem) return;
        if (!supervisorBadge.trim()) {
            alert("Scan the supervisor's badge!");
            return;
        }

        setIsReportingMissing(true);
        try {
            await onReportMissing(currentItem.locationBarcode, currentItem.productSku, missingQty, supervisorBadge);

            // The parent handles clearing the task and fetching the next one on
            // success — this component only needs to close its own submenu.
            setIsMissingMode(false);
            setIsMenuOpen(false);
            setSupervisorBadge('');
        } finally {
            setIsReportingMissing(false);
        }
    };

    const handleDefectSubmit = async () => {
        if (!currentItem) return;
        if (!supervisorBadge.trim()) {
            alert("Scan the supervisor's badge!");
            return;
        }

        setIsReportingDefect(true);
        try {
            await onReportDefect(currentItem.locationBarcode, currentItem.productSku, defectiveQty, supervisorBadge);

            // On success this line is closed out or rerouted server-side, so the
            // refreshed task naturally advances to the next product — nothing else to do here.
            setIsDefectMode(false);
            setIsMenuOpen(false);
            setDefectiveQty(1);
            setSupervisorBadge('');
        } finally {
            setIsReportingDefect(false);
        }
    };

    // ==========================================
    // SCREEN 1: CONTAINER DISPATCH
    // ==========================================
    if (isDispatchMode) {
        return (
            <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)' }}>
                <h3 style={{ margin: '0 0 15px 0', color: '#ff9800', textAlign: 'center' }}>
                    {currentItem ? "Full container (partial hand-off)" : "Task complete!"}
                </h3>

                <p style={{ color: '#aaa', marginBottom: '20px', textAlign: 'center' }}>
                    {expectedContainer ? (
                        <>
                            Scan container{' '}
                            <strong style={{ color: '#ffeb3b', fontSize: '1.1rem' }}>{expectedContainer}</strong>
                            {' '}and then the CONVEYOR.
                        </>
                    ) : (
                        <>Scan the CURRENT container and the CONVEYOR.</>
                    )}
                </p>

                <input
                    type="text"
                    autoFocus
                    title={expectedContainer ? `Scan container ${expectedContainer}` : "Scan the container linked to this task"}
                    placeholder={expectedContainer ? `1. Scan container ${expectedContainer}...` : "1. CONTAINER barcode..."}
                    value={dispatchContainer}
                    onChange={(e) => setDispatchContainer(e.target.value.trim())}
                    style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
                />
                <input type="text" placeholder="2. CONVEYOR barcode..." value={dispatchConveyor} onChange={(e) => setDispatchConveyor(e.target.value.trim())} disabled={!dispatchContainer} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: dispatchContainer ? '#333' : '#222', color: 'white', fontSize: '1.1rem' }} />

                <button onClick={handleDispatchSubmit} disabled={!dispatchContainer || !dispatchConveyor} style={{ width: '100%', padding: '15px', backgroundColor: (dispatchContainer && dispatchConveyor) ? '#4CAF50' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', fontSize: '1.1rem' }}>
                    Confirm dispatch
                </button>

                {currentItem && (
                    <button onClick={() => { setIsDispatchMode(false); setIsMenuOpen(false); }} style={{ width: '100%', padding: '10px', marginTop: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                        Cancel (back to picking)
                    </button>
                )}
            </div>
        );
    }

    // ==========================================
    // SCREEN 2: STANDARD ITEM PICKING
    // ==========================================
    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', position: 'relative' }}>

            <button
                onClick={() => setIsMenuOpen(true)}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Menu)
            </button>

            <h3 style={{ margin: '0 0 10px 0', color: '#4CAF50' }}>Task: {task.id.substring(0, 8)}...</h3>

            {expectedContainer && (
                <p
                    title={`On completion, scan container ${expectedContainer}`}
                    style={{ margin: '0 0 10px 0', color: '#aaa', fontSize: '0.9rem' }}
                >
                    Container: <strong style={{ color: '#ffeb3b' }}>{expectedContainer}</strong>
                </p>
            )}

            <h4 style={{ color: '#ff9800', marginTop: '20px' }}>Current item:</h4>

            <div style={{ borderLeft: '4px solid #ffeb3b', paddingLeft: '10px', marginBottom: '15px', backgroundColor: '#2a2a2a', padding: '15px' }}>
                <p style={{ margin: '5px 0', fontSize: '1.4rem' }}><strong>Location:</strong> <span style={{ color: '#64b5f6' }}>{currentItem?.locationBarcode}</span></p>
                <p style={{ margin: '5px 0', fontSize: '1.2rem' }}><strong>Product:</strong> {currentItem?.productName}</p>
                <p style={{ margin: '5px 0', color: '#a0a0a0' }}>SKU: {currentItem?.productSku}</p>
                <p style={{ margin: '10px 0 5px 0', fontSize: '1.3rem', color: '#ffeb3b' }}>
                    <strong>Pick: {currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 0} pcs</strong>
                </p>
            </div>

            <div style={{ marginTop: '20px', width: '100%', backgroundColor: '#2a2a2a', padding: '15px', borderRadius: '8px', boxSizing: 'border-box' }}>
                {localError && (
                    <div style={{ backgroundColor: '#ff5252', color: 'white', padding: '10px', borderRadius: '4px', marginBottom: '15px', fontWeight: 'bold', textAlign: 'center' }}>
                        {localError}
                    </div>
                )}

                {step === 1 && (
                    <>
                        <input type="text" placeholder="Location barcode..." value={scanLocation} onChange={(e) => setScanLocation(e.target.value.trim())} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }} />
                        <button onClick={handleLocationNext} disabled={!scanLocation} style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: scanLocation ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanLocation ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Check location</button>
                    </>
                )}

                {step === 2 && (
                    <>
                        <input type="text" placeholder="Product SKU..." value={scanSku} onChange={(e) => setScanSku(e.target.value.trim())} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }} />
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(1)} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Back</button>
                            <button onClick={handleSkuNext} disabled={!scanSku} style={{ flex: 2, padding: '15px', backgroundColor: scanSku ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanSku ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Check SKU</button>
                        </div>
                    </>
                )}

                {step === 3 && (
                    <>
                        <div style={{ display: 'flex', gap: '10px', marginBottom: '15px' }}>
                            <label style={{ alignSelf: 'center', fontWeight: 'bold', fontSize: '1.2rem' }}>Qty:</label>
                            <input type="number" min="1" max={currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 1} value={scanQty} onChange={(e) => setScanQty(Number(e.target.value))} style={{ flex: 1, padding: '12px', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }} />
                        </div>
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(2)} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Back</button>
                            <button onClick={handleConfirm} style={{ flex: 2, padding: '15px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>Into container</button>
                        </div>
                    </>
                )}
            </div>

            {/* ITEM LIST (OVERVIEW) */}
            {isOverviewOpen && (
                <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', backgroundColor: '#1e1e1e', borderRadius: '8px', display: 'flex', flexDirection: 'column', padding: '20px', boxSizing: 'border-box', zIndex: 20 }}>
                    <h3 style={{ color: '#4CAF50', margin: '0 0 15px 0', textAlign: 'center' }}>Order overview</h3>
                    <div style={{ flex: 1, overflowY: 'auto', marginBottom: '15px', paddingRight: '5px' }}>
                        {task.items.map(item => {
                            const isDone = item.pickedQuantity >= item.requiredQuantity;
                            const isPartial = item.pickedQuantity > 0 && !isDone;
                            return (
                                <div key={item.id} style={{ borderLeft: `5px solid ${isDone ? '#4CAF50' : isPartial ? '#ff9800' : '#555'}`, backgroundColor: '#2a2a2a', padding: '12px', marginBottom: '10px', borderRadius: '4px' }}>
                                    <p style={{ margin: '0 0 5px 0', fontSize: '1.1rem' }}><strong>Loc:</strong> <span style={{ color: '#64b5f6' }}>{item.locationBarcode}</span></p>
                                    <p style={{ margin: '0 0 5px 0' }}>{item.productName}</p>
                                    <p style={{ margin: '0 0 8px 0', color: '#aaa', fontSize: '0.9rem' }}>SKU: {item.productSku}</p>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <span style={{ fontSize: '1.2rem', fontWeight: 'bold', color: isDone ? '#4CAF50' : '#fff' }}>Picked: {item.pickedQuantity} / {item.requiredQuantity}</span>
                                        {isDone && <span style={{ color: '#4CAF50', fontWeight: 'bold' }}>✓ Done</span>}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                    <button onClick={() => setIsOverviewOpen(false)} style={{ width: '100%', padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', cursor: 'pointer', fontWeight: 'bold' }}>Back to picking</button>
                </div>
            )}

            {/* ESC MENU (holds both states inside a single parent) */}
            {isMenuOpen && (
                <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', backgroundColor: 'rgba(0,0,0,0.9)', borderRadius: '8px', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '20px', boxSizing: 'border-box', zIndex: 10 }}>

                    {!isMissingMode && !isDefectMode ? (
                        <>
                            <h3 style={{ color: '#ff5252', marginBottom: '25px', textAlign: 'center' }}>Exceptions menu</h3>

                            <button onClick={() => { setIsDispatchMode(true); setIsMenuOpen(false); }} style={{ width: '100%', padding: '15px', backgroundColor: '#ff9800', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                📦 Full Container
                            </button>

                            <button onClick={() => { setIsOverviewOpen(true); setIsMenuOpen(false); }} style={{ width: '100%', padding: '15px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                📋 Item list
                            </button>

                            <button onClick={() => alert(`Expected stock in the location:\nLocation: ${currentItem?.locationBarcode}\nProduct: ${currentItem?.productSku}\nRemaining: ${currentItem?.availableStock ?? 'unknown'} pcs`)} style={{ width: '100%', padding: '15px', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                🔍 Check shelf stock
                            </button>

                            <button onClick={() => {
                                setIsMissingMode(true);
                                // Prefill with whatever is still left to pick
                                if (currentItem) {
                                    setMissingQty(currentItem.requiredQuantity - currentItem.pickedQuantity);
                                }
                            }} style={{ width: '100%', padding: '15px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                ❌ Item not found
                            </button>

                            <button onClick={() => {
                                setIsDefectMode(true);
                                // Prefill with whatever is still left to pick
                                if (currentItem) {
                                    setDefectiveQty(currentItem.requiredQuantity - currentItem.pickedQuantity);
                                }
                            }} style={{ width: '100%', padding: '15px', backgroundColor: '#795548', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                🔧 Defective / Damaged
                            </button>

                            <button onClick={() => { setIsMenuOpen(false); onCancel(); }} disabled={hasPickedItems} style={{ width: '100%', padding: '15px', backgroundColor: hasPickedItems ? '#333' : '#f44336', color: hasPickedItems ? '#777' : 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', marginBottom: '12px', cursor: hasPickedItems ? 'not-allowed' : 'pointer' }}>
                                🚫 Cancel task {hasPickedItems && '(Unavailable)'}
                            </button>

                            <button onClick={() => setIsMenuOpen(false)} style={{ width: '60%', padding: '10px', marginTop: '30px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                                Close (Esc)
                            </button>
                        </>
                    ) : isMissingMode ? (
                        // SUBMENU: shortage write-off
                        <div style={{ width: '100%', textAlign: 'center' }}>
                            <h3 style={{ color: '#ff5252', marginBottom: '15px' }}>Confirm shortage</h3>

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Quantity to write off (pcs):</p>
                            <input
                                type="number"
                                min="1"
                                max={currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 1}
                                value={missingQty}
                                onChange={(e) => setMissingQty(Number(e.target.value))}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }}
                            />

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan the supervisor's badge:</p>
                            <input
                                type="text"
                                autoFocus
                                placeholder="Supervisor barcode..."
                                value={supervisorBadge}
                                onChange={(e) => setSupervisorBadge(e.target.value.trim())}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem', textAlign: 'center' }}
                            />

                            <div style={{ display: 'flex', gap: '10px' }}>
                                <button onClick={() => setIsMissingMode(false)} disabled={isReportingMissing} style={{ flex: 1, padding: '12px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: isReportingMissing ? 'not-allowed' : 'pointer' }}>Cancel</button>
                                <button onClick={handleMissingSubmit} disabled={isReportingMissing} style={{ flex: 2, padding: '12px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '4px', cursor: isReportingMissing ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}>
                                    {isReportingMissing ? 'Reporting...' : 'Confirm'}
                                </button>
                            </div>
                        </div>
                    ) : (
                        // SUBMENU: defective / damaged write-off
                        <div style={{ width: '100%', textAlign: 'center' }}>
                            <h3 style={{ color: '#ff5252', marginBottom: '15px' }}>Report defective stock</h3>

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Defective quantity at this location (pcs):</p>
                            <input
                                type="number"
                                min="1"
                                max={currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 1}
                                value={defectiveQty}
                                onChange={(e) => setDefectiveQty(Number(e.target.value))}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }}
                            />

                            <p style={{ color: '#888', margin: '0 0 20px 0', fontSize: '0.85rem' }}>
                                These units are removed from stock here and, if possible, replaced automatically
                                from another picking location.
                            </p>

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Scan the supervisor's badge:</p>
                            <input
                                type="text"
                                autoFocus
                                placeholder="Supervisor barcode..."
                                value={supervisorBadge}
                                onChange={(e) => setSupervisorBadge(e.target.value.trim())}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem', textAlign: 'center' }}
                            />

                            <div style={{ display: 'flex', gap: '10px' }}>
                                <button onClick={() => setIsDefectMode(false)} disabled={isReportingDefect} style={{ flex: 1, padding: '12px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: isReportingDefect ? 'not-allowed' : 'pointer' }}>Cancel</button>
                                <button onClick={handleDefectSubmit} disabled={isReportingDefect} style={{ flex: 2, padding: '12px', backgroundColor: '#795548', color: 'white', border: 'none', borderRadius: '4px', cursor: isReportingDefect ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}>
                                    {isReportingDefect ? 'Reporting...' : 'Confirm'}
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
