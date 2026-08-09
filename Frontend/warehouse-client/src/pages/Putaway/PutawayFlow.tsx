import { useState, useEffect } from 'react';
import axiosClient, { SECTOR_STORAGE_KEY } from '../../api/axiosClient';
import type { PutawayTask } from '../../types/putaway';
import ContainerScanScreen from './ContainerScanScreen';
import ActivePutawayScreen from './ActivePutawayScreen';

type Phase = 'LOADING' | 'SCAN' | 'LOOP' | 'DONE';

interface Props {
    sector: string;
    onExitToMenu: () => void;
    onSectorChange: (newSector: string) => void;
}

export default function PutawayFlow({ sector, onExitToMenu, onSectorChange }: Props) {
    const [phase, setPhase] = useState<Phase>('LOADING');
    const [task, setTask] = useState<PutawayTask | null>(null);
    const [scanning, setScanning] = useState<boolean>(false);
    const [finishedContainerBarcode, setFinishedContainerBarcode] = useState<string>('');

    useEffect(() => {
        void checkActiveOnMount();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Resume-on-load, same reasoning as the picking flow: a worker who gets
    // logged out (or hits Escape and comes back) mid-putaway must be able to
    // resume without rescanning the container.
    const checkActiveOnMount = async () => {
        setPhase('LOADING');
        try {
            const response = await axiosClient.get(`/PutawayTask/active?t=${new Date().getTime()}`);
            if (response.data) {
                setTask(response.data);
                setPhase('LOOP');
                return;
            }
        } catch (error) {
            console.error('Error checking for an active putaway task:', error);
        }
        setPhase('SCAN');
    };

    const claimAndEnterLoop = async (containerBarcode: string, targetSector: string) => {
        try {
            const startResponse = await axiosClient.post('/PutawayTask/start', {
                containerBarcode,
                sector: targetSector
            });
            setTask(startResponse.data);
            setPhase('LOOP');
        } catch (error: any) {
            console.error('Error starting putaway:', error);
            alert(error.response?.data || 'Failed to start putaway for this container.');
        }
    };

    const handleScanContainer = async (containerBarcode: string) => {
        setScanning(true);
        try {
            const validateResponse = await axiosClient.post('/PutawayTask/validate-container', {
                containerBarcode,
                sector
            });
            const validation = validateResponse.data;

            if (validation.isValid) {
                await claimAndEnterLoop(containerBarcode, sector);
                return;
            }

            // Sector mismatch is a normal 200 OK outcome, not an error — walk
            // through the exact alert -> confirm sequence this flow requires.
            alert(`This container is from sector ${validation.containerSector}.`);
            const wantsSwitch = window.confirm(`Do you want to change your current sector to ${validation.containerSector}? (Yes/No)`);
            if (wantsSwitch) {
                localStorage.setItem(SECTOR_STORAGE_KEY, validation.containerSector);
                onSectorChange(validation.containerSector);
                await claimAndEnterLoop(containerBarcode, validation.containerSector);
            }
        } catch (error: any) {
            console.error('Error validating container:', error);
            alert(error.response?.data || 'Failed to validate container.');
        } finally {
            setScanning(false);
        }
    };

    const handleConfirmItem = async (locationBarcode: string, productSku: string, quantity: number) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PutawayTask/${task.id}/confirm-item`, {
                locationBarcode,
                productSku,
                quantity
            });
            applyTaskUpdate(response.data);
        } catch (error: any) {
            console.error('Error confirming item:', error);
            alert(error.response?.data || 'Failed to confirm this item.');
        }
    };

    const handleReportMissing = async (locationBarcode: string, productSku: string, missingQuantity: number) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PutawayTask/${task.id}/report-missing`, {
                locationBarcode,
                productSku,
                missingQuantity
            });
            applyTaskUpdate(response.data);
        } catch (error: any) {
            console.error('Error reporting missing item:', error);
            alert(error.response?.data || 'Failed to report the missing item.');
        }
    };

    const applyTaskUpdate = (updatedTask: PutawayTask) => {
        if (updatedTask.status === 'Completed') {
            setFinishedContainerBarcode(updatedTask.containerBarcode);
            setTask(null);
            setPhase('DONE');
        } else {
            setTask(updatedTask);
        }
    };

    if (phase === 'LOADING') {
        return <p>Loading...</p>;
    }

    if (phase === 'SCAN') {
        return (
            <ContainerScanScreen
                sector={sector}
                onScan={handleScanContainer}
                onExitToMenu={onExitToMenu}
                scanning={scanning}
            />
        );
    }

    if (phase === 'LOOP' && task) {
        return (
            <ActivePutawayScreen
                task={task}
                onConfirmItem={handleConfirmItem}
                onReportMissing={handleReportMissing}
            />
        );
    }

    // phase === 'DONE'
    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '30px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', textAlign: 'center' }}>
            <h3 style={{ color: '#4CAF50', marginTop: 0 }}>Putaway of container {finishedContainerBarcode} finished.</h3>
            <button
                onClick={onExitToMenu}
                style={{ width: '100%', padding: '15px', marginTop: '15px', fontSize: '1.1rem', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold' }}
            >
                Return to Main Menu
            </button>
        </div>
    );
}
