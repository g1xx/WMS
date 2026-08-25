import { useState } from 'react';
import PickingGenerator from './PickingGenerator';
import PutawayGenerator from './PutawayGenerator';
import Login from './Login';
import HelpPanel from './HelpPanel';
import { getToken, logout } from './api/axiosClient';
import './App.css';

type Mode = 'orders' | 'receiving';

export default function App() {
    const [mode, setMode] = useState<Mode>('orders');
    const [isLoggedIn, setIsLoggedIn] = useState<boolean>(() => getToken() !== null);

    if (!isLoggedIn) {
        // The panel belongs on the sign-in screen too — this is where a reviewer needs the
        // erp-feed credentials, and without them they cannot get past this screen at all.
        return (
            <>
                <Login onLoggedIn={() => setIsLoggedIn(true)} />
                <HelpPanel />
            </>
        );
    }

    const handleSignOut = () => {
        logout();
        setIsLoggedIn(false);
    };

    return (
        <div className="page">
            <header className="header">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <div>
                        <h1>Inbound Order Feed</h1>
                        <p className="subtitle">
                            Simulates an upstream ERP / marketplace system pushing sales orders and receiving
                            notices into the warehouse — signed in as the feed integration, not a warehouse user.
                        </p>
                    </div>
                    <button className="secondary-btn" onClick={handleSignOut}>Sign out</button>
                </div>
            </header>

            <div className="mode-tabs" role="tablist">
                <button
                    role="tab"
                    aria-selected={mode === 'orders'}
                    className={`mode-tab ${mode === 'orders' ? 'active' : ''}`}
                    onClick={() => setMode('orders')}
                >
                    Sales Orders
                </button>
                <button
                    role="tab"
                    aria-selected={mode === 'receiving'}
                    className={`mode-tab ${mode === 'receiving' ? 'active' : ''}`}
                    onClick={() => setMode('receiving')}
                >
                    Receiving (ASN)
                </button>
            </div>

            {/* Both generators stay mounted at all times - switching tabs only
                toggles visibility, so neither form's in-progress data is ever lost. */}
            <div style={{ display: mode === 'orders' ? 'block' : 'none' }}>
                <PickingGenerator />
            </div>
            <div style={{ display: mode === 'receiving' ? 'block' : 'none' }}>
                <PutawayGenerator />
            </div>

            <HelpPanel />
        </div>
    );
}
