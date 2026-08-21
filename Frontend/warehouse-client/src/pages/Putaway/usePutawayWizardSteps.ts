import { useState } from 'react';
import type { PutawayTask } from '../../types/putaway';

interface UsePutawayWizardStepsArgs {
    task: PutawayTask;
    onConfirmItem: (locationBarcode: string, productSku: string, quantity: number) => Promise<void>;
}

// The location-first, then product+quantity scan sequence for a single putaway
// line: which location is locked in, the soft-validation confirm for a
// non-suggested address, and the SKU/quantity confirmation. currentItem/remaining/
// suggestedLocations are derived here from the task so the screen component just
// renders whatever this hook currently holds.
export function usePutawayWizardSteps({ task, onConfirmItem }: UsePutawayWizardStepsArgs) {
    const [locationInput, setLocationInput] = useState<string>('');
    const [scannedLocation, setScannedLocation] = useState<string>('');

    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

    const [localError, setLocalError] = useState<string>('');
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    const currentItem = task.items.find(i => i.putAwayQuantity + i.missingQuantity < i.expectedQuantity);
    const remaining = currentItem ? currentItem.expectedQuantity - currentItem.putAwayQuantity - currentItem.missingQuantity : 0;
    const suggestedLocations = currentItem?.suggestedLocations ?? [];

    const resetToLocationStep = () => {
        setLocationInput('');
        setScannedLocation('');
        setScanSku('');
        setScanQty(1);
        setLocalError('');
    };

    const confirmLocation = () => {
        const trimmed = locationInput.trim();
        if (!trimmed) return;

        const isSuggested = suggestedLocations.some(l => l.locationBarcode === trimmed);
        if (!isSuggested) {
            const confirmed = window.confirm(
                'Данного адреса нет в списке рекомендованных. Уверены, что хотите положить товар сюда?'
            );
            if (!confirmed) return;
        }

        setScannedLocation(trimmed);
        setScanQty(remaining);
        setLocalError('');
    };

    const changeLocation = () => {
        setScannedLocation('');
        setLocationInput('');
    };

    const confirmItem = async () => {
        if (!currentItem || !scannedLocation) return;

        if (scanSku.trim() !== currentItem.productSku.trim()) {
            setLocalError(`Wrong item! Expected: ${currentItem.productSku}`);
            return;
        }

        setIsSubmitting(true);
        try {
            await onConfirmItem(scannedLocation, currentItem.productSku, scanQty);
            // Back to Step 1 for whatever the next item turns out to be.
            resetToLocationStep();
        } finally {
            setIsSubmitting(false);
        }
    };

    return {
        currentItem,
        remaining,
        suggestedLocations,
        locationInput, setLocationInput,
        scannedLocation,
        scanSku, setScanSku,
        scanQty, setScanQty,
        localError,
        isSubmitting,
        resetToLocationStep,
        confirmLocation,
        changeLocation,
        confirmItem,
    };
}
