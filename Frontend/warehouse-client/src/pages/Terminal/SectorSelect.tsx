import { useState } from 'react';
import { SECTOR_STORAGE_KEY } from '../../api/axiosClient';

interface Props {
    onConfirm: (sector: string) => void;
    onBack: () => void;
    onEscape: () => void;
}

export default function SectorSelect({ onConfirm, onBack, onEscape }: Props) {
    // Pre-fill with whatever was last saved, so "Change Sector" is a quick edit, not a blank restart
    const [sector, setSector] = useState<string>(() => localStorage.getItem(SECTOR_STORAGE_KEY) ?? '');

    const handleConfirm = () => {
        const trimmed = sector.trim();
        if (!trimmed) {
            alert('Please enter a sector (e.g. mp1, mr1).');
            return;
        }
        onConfirm(trimmed);
    };

    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '30px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', textAlign: 'center', position: 'relative' }}>
            <button
                onClick={onEscape}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Menu)
            </button>

            <h2 style={{ color: '#4CAF50', marginTop: 0 }}>Select Sector</h2>
            <p style={{ color: '#aaa', marginBottom: '25px' }}>
                Enter the sector you are picking in (e.g. mp1, mr1):
            </p>

            <input
                type="text"
                autoFocus
                placeholder="Sector (e.g. mp1, mr1)"
                value={sector}
                onChange={(e) => setSector(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleConfirm(); }}
                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem', textAlign: 'center' }}
            />

            <button
                onClick={handleConfirm}
                style={{ width: '100%', padding: '18px', fontSize: '1.2rem', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold' }}
            >
                Confirm Sector
            </button>

            <button
                onClick={onBack}
                style={{ width: '100%', padding: '10px', marginTop: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
            >
                Back
            </button>
        </div>
    );
}
