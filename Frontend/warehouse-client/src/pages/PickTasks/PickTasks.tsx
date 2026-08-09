import { useState, useEffect } from 'react';
import axiosClient, { fetchSupervisorAuthHeader, isSupervisorAuthError } from '../../api/axiosClient';
import type { PickTask } from '../../types/task';
import NewTaskScreen from './NewTaskScreen';
import ActiveTaskScreen from './ActiveTaskScreen';

interface Props {
    sector: string;
    onExitToMenu: () => void;
}

export default function PickTasks({ sector, onExitToMenu }: Props) {
    const [task, setTask] = useState<PickTask | null>(null);
    const [taskLoading, setTaskLoading] = useState<boolean>(true);

    const [containerBarcode, setContainerBarcode] = useState<string>('');
    const [scanLocation, setScanLocation] = useState<string>('');
    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

    useEffect(() => {
        void fetchTask();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Escape returns to the terminal MENU, but ONLY when ActiveTaskScreen isn't
    // already mounted: it has its own window keydown listener for Escape (its
    // exceptions menu), and firing both on the same keypress would open that
    // menu AND boot the worker out to MENU at once. So this only applies to the
    // "no tasks" empty state and the container-scan (NewTaskScreen) state.
    useEffect(() => {
        if (task && task.status !== 'New') return; // ActiveTaskScreen owns Escape here

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key !== 'Escape') return;
            onExitToMenu();
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [task, onExitToMenu]);

    // Always checks for the worker's own in-flight task first, independent of any
    // other state — this is what lets a re-login (or a page reload) resume
    // straight back into it, and only falls back to requesting a fresh one
    // (scoped to the current sector) if nothing is in progress.
    const fetchTask = async () => {
        setTaskLoading(true);
        try {
            const activeResponse = await axiosClient.get(`/PickTask/active?t=${new Date().getTime()}`);
            if (activeResponse.data) {
                setTask(activeResponse.data);
                setContainerBarcode('');
                return;
            }

            const nextResponse = await axiosClient.get(`/PickTask/next?sector=${encodeURIComponent(sector)}&t=${new Date().getTime()}`);
            setTask(nextResponse.data ? nextResponse.data : null);
            setContainerBarcode('');
        } catch (error) {
            console.error('Error fetching task:', error);
            alert('Failed to load task.');
        } finally {
            setTaskLoading(false);
        }
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

    const handleReportMissing = async (locationBarcode: string, productSku: string, missingQuantity: number, supervisorBadge: string) => {
        if (!task) return;
        try {
            const elevatedConfig = await fetchSupervisorAuthHeader(supervisorBadge);

            const response = await axiosClient.post(`/PickTask/${task.id}/report-missing`, {
                locationBarcode,
                productSku,
                missingQuantity
            }, elevatedConfig);

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
            if (isSupervisorAuthError(error)) {
                alert("Supervisor authorization failed: Invalid badge or missing permissions.");
                return;
            }
            console.error("Shortage write-off error:", error);
            alert(error.response?.data || "Failed to confirm the shortage.");
        }
    };

    const handleReportDefect = async (locationBarcode: string, productSku: string, defectiveQuantity: number, supervisorBadge: string) => {
        if (!task) return;
        try {
            const elevatedConfig = await fetchSupervisorAuthHeader(supervisorBadge);

            const response = await axiosClient.post(`/PickTask/${task.id}/report-defect`, {
                locationBarcode,
                productSku,
                defectiveQuantity
            }, elevatedConfig);

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
            if (isSupervisorAuthError(error)) {
                alert("Supervisor authorization failed: Invalid badge or missing permissions.");
                return;
            }
            console.error("Error reporting defect:", error);
            alert(error.response?.data || "Failed to report the defect.");
        }
    };

    if (taskLoading) {
        return <p>Loading task...</p>;
    }

    if (!task) {
        return (
            <div style={{ backgroundColor: '#1e1e1e', padding: '30px', borderRadius: '8px', width: '90%', maxWidth: '400px', textAlign: 'center', position: 'relative' }}>
                <button
                    onClick={onExitToMenu}
                    style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
                >
                    ESC (Menu)
                </button>
                <p style={{ color: '#aaa' }}>No tasks available in sector {sector}</p>
                <button
                    onClick={() => fetchTask()}
                    style={{ width: '100%', padding: '12px', marginTop: '15px', fontSize: '0.9rem', backgroundColor: '#333', color: '#aaa', border: '1px solid #555', borderRadius: '4px', cursor: 'pointer' }}
                >
                    Check again
                </button>
            </div>
        );
    }

    if (task.status === 'New') {
        return (
            <NewTaskScreen
                task={task}
                containerBarcode={containerBarcode}
                setContainerBarcode={setContainerBarcode}
                onStartTask={handleStartTask}
            />
        );
    }

    return (
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
    );
}
