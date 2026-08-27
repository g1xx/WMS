import { useState, useEffect } from 'react';
import axiosClient, { SECTOR_STORAGE_KEY } from '../../api/axiosClient';
import type { PickTask } from '../../types/task';
import type { PutawayTask } from '../../types/putaway';
import MainMenu from './MainMenu';
import SectorSelect from './SectorSelect';
import PickTasks from '../PickTasks/PickTasks';
import PutawayFlow from '../Putaway/PutawayFlow';
import RelocationFlow from '../Relocation/RelocationFlow';

type Screen = 'LOADING' | 'MENU' | 'SECTOR_SELECT' | 'PICKING' | 'PUTAWAY' | 'RELOCATION';
type PendingFlow = 'PICKING' | 'PUTAWAY' | null;

export default function Terminal() {
    const [screen, setScreen] = useState<Screen>('LOADING');
    const [sector, setSector] = useState<string>('');
    // Which flow SECTOR_SELECT should hand off to once a sector is confirmed —
    // null when the user got there via "Change Sector" from the menu, in which
    // case confirming just returns to MENU rather than committing to a flow.
    const [pendingFlow, setPendingFlow] = useState<PendingFlow>(null);

    useEffect(() => {
        void resumeOnLoad();
    }, []);

    // On load, check both flows for the worker's own in-flight task, independent
    // of sector — this is what lets a re-login resume straight back into it.
    const resumeOnLoad = async () => {
        setScreen('LOADING');

        try {
            const pickResponse = await axiosClient.get<PickTask | null>(`/PickTask/active?t=${new Date().getTime()}`);
            if (pickResponse.data) {
                setSector(pickResponse.data.sector);
                setScreen('PICKING');
                return;
            }
        } catch (error) {
            console.error('Error checking for an active pick task:', error);
        }

        try {
            const putawayResponse = await axiosClient.get<PutawayTask | null>(`/PutawayTask/active?t=${new Date().getTime()}`);
            if (putawayResponse.data) {
                setSector(putawayResponse.data.sector);
                setScreen('PUTAWAY');
                return;
            }
        } catch (error) {
            console.error('Error checking for an active putaway task:', error);
        }

        setScreen('MENU');
    };

    const returnToMenu = () => {
        setScreen('MENU');
    };

    // MENU: "Start Picking" / "Start Putaway" — resume the saved sector if there
    // is one, otherwise ask for it first and come back to this flow afterwards.
    const startFlow = (flow: 'PICKING' | 'PUTAWAY') => {
        const savedSector = localStorage.getItem(SECTOR_STORAGE_KEY);
        if (!savedSector) {
            setPendingFlow(flow);
            setScreen('SECTOR_SELECT');
            return;
        }
        setSector(savedSector);
        setScreen(flow);
    };

    // MENU: "Change Sector" — just updates the saved sector and returns to MENU,
    // it does not commit to either flow.
    const handleChangeSector = () => {
        setPendingFlow(null);
        setScreen('SECTOR_SELECT');
    };

    const handleSectorConfirm = (chosenSector: string) => {
        localStorage.setItem(SECTOR_STORAGE_KEY, chosenSector);
        setSector(chosenSector);

        if (pendingFlow) {
            setScreen(pendingFlow);
            setPendingFlow(null);
        } else {
            setScreen('MENU');
        }
    };

    if (screen === 'LOADING') {
        return <h2 style={{ color: 'white', textAlign: 'center', marginTop: '50px' }}>Loading...</h2>;
    }

    return (
        <div style={{ backgroundColor: '#121212', minHeight: '100vh', color: '#e0e0e0', padding: '20px' }}>
            <h2 style={{ textAlign: 'center', marginBottom: '10px' }}>Warehouse Terminal</h2>
            {(screen === 'PICKING' || screen === 'PUTAWAY') && sector && (
                <p style={{ textAlign: 'center', color: '#888', marginBottom: '20px' }}>Sector: <strong style={{ color: '#64b5f6' }}>{sector}</strong></p>
            )}

            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', alignItems: 'center' }}>
                {screen === 'MENU' && (
                    <MainMenu
                        onStartPicking={() => startFlow('PICKING')}
                        onStartPutaway={() => startFlow('PUTAWAY')}
                        // Straight in, no sector step: relocation is driven entirely by
                        // scanned location barcodes rather than scoped to a picking zone.
                        onStartRelocation={() => setScreen('RELOCATION')}
                        onChangeSector={handleChangeSector}
                    />
                )}

                {screen === 'SECTOR_SELECT' && (
                    <SectorSelect onConfirm={handleSectorConfirm} onBack={returnToMenu} onEscape={returnToMenu} />
                )}

                {screen === 'PICKING' && (
                    <PickTasks sector={sector} onExitToMenu={returnToMenu} />
                )}

                {screen === 'PUTAWAY' && (
                    <PutawayFlow sector={sector} onExitToMenu={returnToMenu} onSectorChange={setSector} />
                )}

                {screen === 'RELOCATION' && (
                    <RelocationFlow onExit={returnToMenu} />
                )}
            </div>
        </div>
    );
}
