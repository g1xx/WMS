import { useState, useEffect } from 'react';
import axiosClient, { SECTOR_STORAGE_KEY } from '../../api/axiosClient';
import type { PickTask } from '../../types/task';
import MainMenu from './MainMenu';
import SectorSelect from './SectorSelect';
import NewTaskScreen from './NewTaskScreen';
import ActiveTaskScreen from './ActiveTaskScreen';

type Screen = 'LOADING' | 'MENU' | 'SECTOR_SELECT' | 'PICKING';

export default function PickTasks() {
    const [screen, setScreen] = useState<Screen>('LOADING');
    const [sector, setSector] = useState<string>('');
    const [task, setTask] = useState<PickTask | null>(null);
    const [taskLoading, setTaskLoading] = useState<boolean>(false);

    const [containerBarcode, setContainerBarcode] = useState<string>('');
    const [scanLocation, setScanLocation] = useState<string>('');
    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

    useEffect(() => {
        void resumeOrShowMenu();
    }, []);

    // Escape returns PICKING -> MENU, but ONLY when ActiveTaskScreen isn't already
    // mounted: it has its own window keydown listener for Escape (its exceptions
    // menu), and firing both on the same keypress would open that menu AND boot
    // the worker out to MENU at once. So this only applies to the "no tasks"
    // empty state and the container-scan (NewTaskScreen) state.
    useEffect(() => {
        if (screen !== 'PICKING') return;
        if (task && task.status !== 'New') return; // ActiveTaskScreen owns Escape here

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key !== 'Escape') return;
            setTask(null);
            setScreen('MENU');
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [screen, task]);

    // Always checks for the worker's own in-flight task first, independent of any
    // sector — this is what lets a re-login resume straight back into the task.
    const resumeOrShowMenu = async () => {
        setScreen('LOADING');
        try {
            const response = await axiosClient.get(`/PickTask/active?t=${new Date().getTime()}`);
            if (response.data) {
                setTask(response.data);
                setSector(response.data.sector);
                setContainerBarcode('');
                setScreen('PICKING');
                return;
            }
        } catch (error) {
            console.error('Error checking for an active task:', error);
        }
        setScreen('MENU');
    };

    // Same resume-first logic, used for every refresh once picking has started:
    // check for the worker's own active task, and only fall back to requesting a
    // fresh one (scoped to the current sector) if nothing is in progress.
    const fetchTask = async (sectorOverride?: string) => {
        const targetSector = (sectorOverride ?? sector).trim();

        setTaskLoading(true);
        try {
            const activeResponse = await axiosClient.get(`/PickTask/active?t=${new Date().getTime()}`);
            if (activeResponse.data) {
                setTask(activeResponse.data);
                setSector(activeResponse.data.sector);
                setContainerBarcode('');
                return;
            }

            if (!targetSector) {
                setTask(null);
                return;
            }

            const nextResponse = await axiosClient.get(`/PickTask/next?sector=${encodeURIComponent(targetSector)}&t=${new Date().getTime()}`);
            setTask(nextResponse.data ? nextResponse.data : null);
            setContainerBarcode('');
        } catch (error) {
            console.error('Error fetching task:', error);
            alert('Failed to load task.');
        } finally {
            setTaskLoading(false);
        }
    };

    // MENU: "Start Picking" — resume the saved sector if there is one, otherwise ask for it
    const handleStartPicking = async () => {
        const savedSector = localStorage.getItem(SECTOR_STORAGE_KEY);
        if (!savedSector) {
            setScreen('SECTOR_SELECT');
            return;
        }
        setSector(savedSector);
        setScreen('PICKING');
        await fetchTask(savedSector);
    };

    // MENU: "Change Sector"
    const handleChangeSector = () => {
        setScreen('SECTOR_SELECT');
    };

    // SECTOR_SELECT: confirm — persists so it survives reloads/battery deaths
    const handleSectorConfirm = async (chosenSector: string) => {
        localStorage.setItem(SECTOR_STORAGE_KEY, chosenSector);
        setSector(chosenSector);
        setScreen('PICKING');
        await fetchTask(chosenSector);
    };

    const handleStartTask = async () => {
        if (!task || !containerBarcode) {
            alert("Please scan a container barcode first!");
            return;
        }
        try {
            await axiosClient.post(`/PickTask/${task.id}/start`, {
                containerBarcode: containerBarcode
            });
            setTask({ ...task, status: 'InProgress' });
        } catch (error: any) {
            console.error("Error starting task:", error);
            alert(error.response?.data || "Failed to start task.");
            await fetchTask();
        }
    };

    const handlePickItem = async () => {
        if (!task) return;
        try {
            await axiosClient.post(`/PickTask/${task.id}/pick`, {
                locationBarcode: scanLocation,
                productSku: scanSku,
                quantity: scanQty
            });

            // No alert here on purpose: confirming "OK" after every item slows the worker down

            setScanLocation('');
            setScanSku('');
            setScanQty(1);

            // Must wait for the refreshed data before continuing
            await fetchTask();
        } catch (error: any) {
            console.error("Error picking item:", error);
            alert(error.response?.data || "Scan error!");
        }
    };

    const handleDispatch = async (containerBarcode: string, conveyorBarcode: string) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/dispatch`, {
                containerBarcode: containerBarcode,
                conveyorBarcode: conveyorBarcode
            });

            alert(response.data?.message || "Container successfully sent to the conveyor.");

            await fetchTask();
        } catch (error: any) {
            console.error("Error dispatching task:", error);
            alert(error.response?.data || "Failed to close the container.");
        }
    };

    const handleCancelTask = async () => {
        if (!task) return;

        const confirmBox = window.confirm("Are you sure you want to give up this task? The container will be unlinked.");
        if (!confirmBox) return;

        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/cancel`);
            alert(response.data?.message || "Task cancelled.");
            await fetchTask();
        } catch (error: any) {
            console.error("Error canceling task:", error);
            alert(error.response?.data || "Failed to cancel the task.");
        }
    };

    const handleReportMissing = async (locationBarcode: string, productSku: string, missingQuantity: number, brigadierBarcode: string) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/report-missing`, {
                locationBarcode,
                productSku,
                missingQuantity,
                brigadierBarcode
            });

            alert(response.data?.message || "Shortage confirmed.");

            // Same reasoning as the defect handler below: a shortage write-off can
            // close out the line (or the whole task) server-side, so don't trust
            // the task/item that was on screen a moment ago. Clear everything
            // immediately and only then ask the server what to work on next.
            setTask(null);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setContainerBarcode('');

            await fetchTask();
        } catch (error: any) {
            console.error("Shortage write-off error:", error);
            alert(error.response?.data || "Failed to confirm the shortage.");
        }
    };

    const handleReportDefect = async (locationBarcode: string, productSku: string, defectiveQuantity: number) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/report-defect`, {
                locationBarcode,
                productSku,
                defectiveQuantity
            });

            alert(response.data?.message || "Defect reported.");

            // Do not trust the task/item we had on screen a moment ago: the line this
            // defect was reported against may now be closed out or rerouted to a
            // different pick task entirely. Clear everything immediately and only
            // then ask the server what to work on next, rather than risking another
            // action (e.g. a scan) racing against a task that's no longer active.
            setTask(null);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setContainerBarcode('');

            await fetchTask();
        } catch (error: any) {
            console.error("Error reporting defect:", error);
            alert(error.response?.data || "Failed to report the defect.");
        }
    };

    if (screen === 'LOADING') {
        return <h2 style={{ color: 'white', textAlign: 'center', marginTop: '50px' }}>Loading...</h2>;
    }

    return (
        <div style={{ backgroundColor: '#121212', minHeight: '100vh', color: '#e0e0e0', padding: '20px' }}>
            <h2 style={{ textAlign: 'center', marginBottom: '10px' }}>Picking Terminal</h2>
            {screen === 'PICKING' && sector && (
                <p style={{ textAlign: 'center', color: '#888', marginBottom: '20px' }}>Sector: <strong style={{ color: '#64b5f6' }}>{sector}</strong></p>
            )}

            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', alignItems: 'center' }}>
                {screen === 'MENU' ? (
                    <MainMenu onStartPicking={handleStartPicking} onChangeSector={handleChangeSector} />
                ) : screen === 'SECTOR_SELECT' ? (
                    <SectorSelect onConfirm={handleSectorConfirm} onBack={() => setScreen('MENU')} />
                ) : taskLoading ? (
                    <p>Loading task...</p>
                ) : !task ? (
                    <div style={{ backgroundColor: '#1e1e1e', padding: '30px', borderRadius: '8px', width: '90%', maxWidth: '400px', textAlign: 'center' }}>
                        <p style={{ color: '#aaa' }}>No tasks available in sector {sector}</p>
                        <button
                            onClick={() => fetchTask()}
                            style={{ width: '100%', padding: '12px', marginTop: '15px', fontSize: '0.9rem', backgroundColor: '#333', color: '#aaa', border: '1px solid #555', borderRadius: '4px', cursor: 'pointer' }}
                        >
                            Check again
                        </button>
                    </div>
                ) : task.status === 'New' ? (
                    <NewTaskScreen
                        task={task}
                        containerBarcode={containerBarcode}
                        setContainerBarcode={setContainerBarcode}
                        onStartTask={handleStartTask}
                    />
                ) : (
                    <ActiveTaskScreen
                        task={task}
                        scanLocation={scanLocation}
                        setScanLocation={setScanLocation}
                        scanSku={scanSku}
                        setScanSku={setScanSku}
                        scanQty={scanQty}
                        setScanQty={setScanQty}
                        onPickItem={handlePickItem}
                        onDispatch={handleDispatch}
                        onCancel={handleCancelTask}
                        onReportDefect={handleReportDefect}
                        onReportMissing={handleReportMissing}
                    />
                )}
            </div>
        </div>
    );
}
