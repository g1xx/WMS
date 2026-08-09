import { useState } from 'react';

interface Props {
    sector: string;
    onScan: (containerBarcode: string) => void;
    onExitToMenu: () => void;
    scanning: boolean;
}

export default function ContainerScanScreen({ sector, onScan, onExitToMenu, scanning }: Props) {
    const [containerBarcode, setContainerBarcode] = useState<string>('');

    const handleScan = () => {
        const trimmed = containerBarcode.trim();
        if (!trimmed) {
            alert('Please scan a container barcode first!');
            return;
        }
        onScan(trimmed);
    };

    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', position: 'relative' }}>
            <button
                onClick={onExitToMenu}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Menu)
            </button>

            <h3 style={{ margin: '0 0 10px 0', color: '#ff9800' }}>Putaway</h3>
            <p style={{ color: '#aaa', marginBottom: '20px' }}>Sector: <strong style={{ color: '#64b5f6' }}>{sector}</strong></p>

            <input
                type="text"
                autoFocus
                placeholder="Scan Container Barcode"
                value={containerBarcode}
                onChange={(e) => setContainerBarcode(e.target.value.trim())}
                onKeyDown={(e) => { if (e.key === 'Enter') handleScan(); }}
                disabled={scanning}
                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }}
            />

            <button
                onClick={handleScan}
                disabled={scanning}
                style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: scanning ? '#555' : '#2196F3', color: 'white', border: 'none', borderRadius: '4px', cursor: scanning ? 'not-allowed' : 'pointer', fontWeight: 'bold' }}
            >
                {scanning ? 'Checking...' : 'Scan Container'}
            </button>
        </div>
    );
}
