import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { SECTOR_STORAGE_KEY, alertIfSupervisorAuthError, extractErrorMessage } from '../../api/axiosClient';
import { queryKeys } from '../../api/queryKeys';
import {
    fetchActivePutawayTask,
    validateContainer,
    startPutaway,
    confirmPutawayItem,
    reportPutawayMissing,
} from '../../api/putawayApi';
import type { PutawayTask } from '../../types/putaway';
import ContainerScanScreen from './ContainerScanScreen';
import ActivePutawayScreen from './ActivePutawayScreen';
import RelocationFlow from '../Relocation/RelocationFlow';

type Phase = 'LOADING' | 'SCAN' | 'LOOP' | 'DONE';

interface Props {
    sector: string;
    onExitToMenu: () => void;
    onSectorChange: (newSector: string) => void;
}

export default function PutawayFlow({ sector, onExitToMenu, onSectorChange }: Props) {
    const queryClient = useQueryClient();

    const [phase, setPhase] = useState<Phase>('LOADING');
    const [task, setTask] = useState<PutawayTask | null>(null);
    const [finishedContainerBarcode, setFinishedContainerBarcode] = useState<string>('');

    // Relocation entered from the putaway Esc menu. Overlays the task rather than
    // replacing it, so the wizard's in-progress step survives the round trip.
    const [isRelocating, setIsRelocating] = useState<boolean>(false);

    // Resume-on-load, same reasoning as the picking flow: a worker who gets
    // logged out (or hits Escape and comes back) mid-putaway must be able to
    // resume without rescanning the container. This is a one-shot check — once
    // resolved, the mutations below own every further state transition, so the
    // query is disabled again immediately (staleTime: Infinity, enabled while
    // still LOADING only) rather than kept live in the background.
    const { data: resumedTask, isFetched } = useQuery({
        queryKey: queryKeys.putawayTask.active,
        queryFn: fetchActivePutawayTask,
        enabled: phase === 'LOADING',
        staleTime: Infinity,
    });

    useEffect(() => {
        if (phase !== 'LOADING' || !isFetched) return;

        if (resumedTask) {
            setTask(resumedTask);
            setPhase('LOOP');
        } else {
            setPhase('SCAN');
        }
    }, [phase, isFetched, resumedTask]);

    const scanContainerMutation = useMutation({
        mutationFn: async (scannedBarcode: string) => {
            const validation = await validateContainer(scannedBarcode, sector);

            if (validation.isValid) {
                const startedTask = await startPutaway(scannedBarcode, sector);
                return { task: startedTask, switchedSector: null as string | null };
            }

            // Sector mismatch is a normal 200 OK outcome, not an error — walk
            // through the exact alert -> confirm sequence this flow requires.
            alert(`This container is from sector ${validation.containerSector}.`);
            const wantsSwitch = window.confirm(`Do you want to change your current sector to ${validation.containerSector}? (Yes/No)`);
            if (!wantsSwitch) {
                return null;
            }

            localStorage.setItem(SECTOR_STORAGE_KEY, validation.containerSector);
            const startedTask = await startPutaway(scannedBarcode, validation.containerSector);
            return { task: startedTask, switchedSector: validation.containerSector };
        },
        onSuccess: (result) => {
            if (!result) return; // user declined the sector switch — stay on SCAN

            if (result.switchedSector) {
                onSectorChange(result.switchedSector);
            }

            setTask(result.task);
            setPhase('LOOP');
            queryClient.setQueryData(queryKeys.putawayTask.active, result.task);
            void queryClient.invalidateQueries({ queryKey: queryKeys.putawayTask.active });
        },
        onError: (error: unknown) => {
            console.error('Error validating/starting putaway:', error);
            alert(extractErrorMessage(error, 'Failed to validate or start putaway for this container.'));
        },
    });

    // Shared by confirm-item and report-missing: both endpoints return the fresh
    // PutawayTask directly, so the response IS the new truth — no extra round trip
    // needed, just keep the query cache in sync with it.
    const applyTaskUpdate = (updatedTask: PutawayTask) => {
        if (updatedTask.status === 'Completed') {
            setFinishedContainerBarcode(updatedTask.containerBarcode);
            setTask(null);
            setPhase('DONE');
            queryClient.setQueryData(queryKeys.putawayTask.active, null);
        } else {
            setTask(updatedTask);
            queryClient.setQueryData(queryKeys.putawayTask.active, updatedTask);
        }
        void queryClient.invalidateQueries({ queryKey: queryKeys.putawayTask.active });
    };

    const confirmItemMutation = useMutation({
        mutationFn: ({ locationBarcode, productSku, quantity }: { locationBarcode: string; productSku: string; quantity: number }) => {
            if (!task) throw new Error('No active putaway task.');
            return confirmPutawayItem(task.id, locationBarcode, productSku, quantity);
        },
        onSuccess: applyTaskUpdate,
        onError: (error: unknown) => {
            console.error('Error confirming item:', error);
            alert(extractErrorMessage(error, 'Failed to confirm this item.'));
        },
    });

    const reportMissingMutation = useMutation({
        mutationFn: ({ productSku, missingQuantity, supervisorBadge }: { productSku: string; missingQuantity: number; supervisorBadge: string }) => {
            if (!task) throw new Error('No active putaway task.');
            return reportPutawayMissing(task.id, productSku, missingQuantity, supervisorBadge);
        },
        onSuccess: applyTaskUpdate,
        onError: (error: unknown) => {
            if (alertIfSupervisorAuthError(error)) return;
            console.error('Error reporting missing item:', error);
            alert(extractErrorMessage(error, 'Failed to report the missing item.'));
        },
    });

    if (phase === 'LOADING') {
        return <p>Loading...</p>;
    }

    if (phase === 'SCAN') {
        return (
            <ContainerScanScreen
                sector={sector}
                onScan={(containerBarcode) => scanContainerMutation.mutate(containerBarcode)}
                onExitToMenu={onExitToMenu}
                scanning={scanContainerMutation.isPending}
            />
        );
    }

    if (phase === 'LOOP' && task) {
        // ActivePutawayScreen is HIDDEN, never unmounted, while relocating. That is the
        // whole resume mechanism: usePutawayWizardSteps holds the sub-step (which location
        // is locked in, the typed SKU and quantity) in component state, so unmounting would
        // discard it and drop the worker back at the location step. Which ITEM they're on
        // is derived from the server task and needs nothing preserved.
        //
        // Same technique, and same reason, as TestOrderGenerator's App.tsx keeping both
        // generators mounted behind a display toggle.
        return (
            <>
                <div style={{ display: isRelocating ? 'none' : 'contents' }}>
                    <ActivePutawayScreen
                        task={task}
                        menuEnabled={!isRelocating}
                        onOpenRelocation={() => setIsRelocating(true)}
                        onExitToMenu={onExitToMenu}
                        onConfirmItem={async (locationBarcode, productSku, quantity) => {
                            await confirmItemMutation.mutateAsync({ locationBarcode, productSku, quantity }).catch(() => {});
                        }}
                        onReportMissing={async (missingQuantity, supervisorBadge) => {
                            const currentItem = task.items.find(i => i.putAwayQuantity + i.missingQuantity < i.expectedQuantity);
                            if (!currentItem) return;
                            await reportMissingMutation.mutateAsync({ productSku: currentItem.productSku, missingQuantity, supervisorBadge }).catch(() => {});
                        }}
                    />
                </div>

                {isRelocating && (
                    <RelocationFlow
                        exitLabel="Back to putaway"
                        // Goes through RelocationFlow's own guard, so returning to putaway
                        // is blocked while transit still holds stock — exactly as leaving
                        // to the main menu is.
                        onExit={() => setIsRelocating(false)}
                    />
                )}
            </>
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
