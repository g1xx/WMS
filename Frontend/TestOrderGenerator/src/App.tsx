import { useState } from 'react';
import PickingGenerator from './PickingGenerator';
import PutawayGenerator from './PutawayGenerator';
import './App.css';

type Mode = 'picking' | 'putaway';

export default function App() {
    const [mode, setMode] = useState<Mode>('picking');

    return (
        <div className="page">
            <header className="header">
                <h1>Test Data Generator</h1>
                <p className="subtitle">Creates picking orders and putaway containers against the live WMS API for testing.</p>
            </header>

            <div className="mode-tabs" role="tablist">
                <button
                    role="tab"
                    aria-selected={mode === 'picking'}
                    className={`mode-tab ${mode === 'picking' ? 'active' : ''}`}
                    onClick={() => setMode('picking')}
                >
                    Picking
                </button>
                <button
                    role="tab"
                    aria-selected={mode === 'putaway'}
                    className={`mode-tab ${mode === 'putaway' ? 'active' : ''}`}
                    onClick={() => setMode('putaway')}
                >
                    Putaway
                </button>
            </div>

            {/* Both generators stay mounted at all times - switching tabs only
                toggles visibility, so neither form's in-progress data is ever lost. */}
            <div style={{ display: mode === 'picking' ? 'block' : 'none' }}>
                <PickingGenerator />
            </div>
            <div style={{ display: mode === 'putaway' ? 'block' : 'none' }}>
                <PutawayGenerator />
            </div>
        </div>
    );
}
