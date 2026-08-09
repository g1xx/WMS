import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { isSupervisorAuthError } from '../../api/axiosClient';
import {
    fetchCurrentPickTask,
    startPickTask,
    pickItem,
    dispatchContainer,
    cancelPickTask,
    reportMissingItem,
    reportDefect,
} from '../../api/pickTaskApi';
import NewTaskScreen from './NewTaskScreen';
import ActiveTaskScreen from './ActiveTaskScreen';

interface Props {
    sector: string;
    onExitToMenu: () => void;
}

const pickTaskQueryKey = (sector: string) => ['pickTask', 'current', sector] as const;

export default function PickTasks({ sector, onExitToMenu }: Props) {
    const queryClient = useQueryClient();

    const [containerBarcode, setContainerBarcode] = useState<string>('');
    const [scanLocation, setScanLocation] = useState<string>('');
    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

    // Always checks for the worker's own in-flight task first, independent of any
    // other state — this is what lets a re-login (or a page reload) resume
    // straight back into it, and only falls back to a fresh one (scoped to the
    // current sector) if nothing is in progress. See fetchCurrentPickTask.
    const {
        data: task,
        isLoading: taskLoading,
        refetch: refetchTask,
    } = useQuery({
        queryKey: pickTaskQueryKey(sector),
        queryFn: () => fetchCurrentPickTask(sector),
    });

    const invalidateTask = () => queryClient.invalidateQueries({ queryKey: pickTaskQueryKey(sector) });

    // Clears any half-scanned container barcode whenever the resolved task changes
    // (new task loaded, task cleared, etc.) — mirrors the original's unconditional
    // reset at the end of every successful fetchTask() call.
    useEffect(() => {
        setContainerBarcode('');
    }, [task]);

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

    const startTaskMutation = useMutation({
        mutationFn: () => {
            if (!task) throw new Error('No task to start.');
            return startPickTask(task.id, containerBarcode);
        },
        onSuccess: () => {
            void invalidateTask();
        },
        onError: (error: any) => {
            console.error('Error starting task:', error);
            alert(error.response?.data || 'Failed to start task.');
            void invalidateTask();
        },
    });

    const pickItemMutation = useMutation({
        mutationFn: () => {
            if (!task) throw new Error('No active task.');
            return pickItem(task.id, scanLocation, scanSku, scanQty);
        },
        onSuccess: () => {
            // No alert here on purpose: confirming "OK" after every item slows the worker down
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            void invalidateTask();
        },
        onError: (error: any) => {
            console.error('Error picking item:', error);
            alert(error.response?.data || 'Scan error!');
        },
    });

    const dispatchMutation = useMutation({
        mutationFn: ({ containerBarcode, conveyorBarcode }: { containerBarcode: string; conveyorBarcode: string }) => {
            if (!task) throw new Error('No active task.');
            return dispatchContainer(task.id, containerBarcode, conveyorBarcode);
        },
        onSuccess: (data) => {
            alert(data?.message || 'Container successfully sent to the conveyor.');
            void invalidateTask();
        },
        onError: (error: any) => {
            console.error('Error dispatching task:', error);
            alert(error.response?.data || 'Failed to close the container.');
        },
    });

    const cancelTaskMutation = useMutation({
        mutationFn: () => {
            if (!task) throw new Error('No active task.');
            return cancelPickTask(task.id);
        },
        onSuccess: (data) => {
            alert(data?.message || 'Task cancelled.');
            void invalidateTask();
        },
        onError: (error: any) => {
            console.error('Error canceling task:', error);
            alert(error.response?.data || 'Failed to cancel the task.');
        },
    });

    const reportMissingMutation = useMutation({
        mutationFn: ({ locationBarcode, productSku, missingQuantity, supervisorBadge }: {
            locationBarcode: string; productSku: string; missingQuantity: number; supervisorBadge: string;
        }) => {
            if (!task) throw new Error('No active task.');
            return reportMissingItem(task.id, locationBarcode, productSku, missingQuantity, supervisorBadge);
        },
        onSuccess: (data) => {
            alert(data?.message || 'Shortage confirmed.');

            // Same reasoning as the defect handler below: a shortage write-off can
            // close out the line (or the whole task) server-side, so don't trust
            // the task/item that was on screen a moment ago. Clear it immediately
            // and only then ask the server what to work on next.
            queryClient.setQueryData(pickTaskQueryKey(sector), null);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setContainerBarcode('');
            void invalidateTask();
        },
        onError: (error: any) => {
            if (isSupervisorAuthError(error)) {
                alert('Supervisor authorization failed: Invalid badge or missing permissions.');
                return;
            }
            console.error('Shortage write-off error:', error);
            alert(error.response?.data || 'Failed to confirm the shortage.');
        },
    });

    const reportDefectMutation = useMutation({
        mutationFn: ({ locationBarcode, productSku, defectiveQuantity, supervisorBadge }: {
            locationBarcode: string; productSku: string; defectiveQuantity: number; supervisorBadge: string;
        }) => {
            if (!task) throw new Error('No active task.');
            return reportDefect(task.id, locationBarcode, productSku, defectiveQuantity, supervisorBadge);
        },
        onSuccess: (data) => {
            alert(data?.message || 'Defect reported.');

            // Do not trust the task/item we had on screen a moment ago: the line this
            // defect was reported against may now be closed out or rerouted to a
            // different pick task entirely. Clear it immediately and only then ask
            // the server what to work on next, rather than risking another action
            // (e.g. a scan) racing against a task that's no longer active.
            queryClient.setQueryData(pickTaskQueryKey(sector), null);
            setScanLocation('');
            setScanSku('');
            setScanQty(1);
            setContainerBarcode('');
            void invalidateTask();
        },
        onError: (error: any) => {
            if (isSupervisorAuthError(error)) {
                alert('Supervisor authorization failed: Invalid badge or missing permissions.');
                return;
            }
            console.error('Error reporting defect:', error);
            alert(error.response?.data || 'Failed to report the defect.');
        },
    });

    const handleStartTask = () => {
        if (!task || !containerBarcode) {
            alert('Please scan a container barcode first!');
            return;
        }
        startTaskMutation.mutate();
    };

    const handleCancelTask = async () => {
        const confirmBox = window.confirm('Are you sure you want to give up this task? The container will be unlinked.');
        if (!confirmBox) return;

        await cancelTaskMutation.mutateAsync().catch(() => {});
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
                    onClick={() => refetchTask()}
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
            onPickItem={async () => {
                await pickItemMutation.mutateAsync().catch(() => {});
            }}
            onDispatch={async (containerBarcode, conveyorBarcode) => {
                await dispatchMutation.mutateAsync({ containerBarcode, conveyorBarcode }).catch(() => {});
            }}
            onCancel={handleCancelTask}
            onReportDefect={async (locationBarcode, productSku, defectiveQuantity, supervisorBadge) => {
                await reportDefectMutation.mutateAsync({ locationBarcode, productSku, defectiveQuantity, supervisorBadge }).catch(() => {});
            }}
            onReportMissing={async (locationBarcode, productSku, missingQuantity, supervisorBadge) => {
                await reportMissingMutation.mutateAsync({ locationBarcode, productSku, missingQuantity, supervisorBadge }).catch(() => {});
            }}
        />
    );
}
