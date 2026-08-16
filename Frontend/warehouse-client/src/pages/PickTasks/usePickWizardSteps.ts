import { useState } from 'react';
import type { PickTaskItem } from '../../types/task';

export type PickWizardStep = 'LOCATION' | 'SKU' | 'QUANTITY';

interface UsePickWizardStepsArgs {
    currentItem: PickTaskItem | undefined;
    scanLocation: string;
    scanSku: string;
    setScanQty: (qty: number) => void;
    onPickItem: () => Promise<void>;
}

// The location -> SKU -> quantity scan sequence for a single pick line, plus its
// wrong-scan validation. scanLocation/scanSku/scanQty stay lifted in the parent
// (PickTasks) since they're reset from outside on mutation success — this hook
// only owns which step the worker is on and the resulting validation error.
export function usePickWizardSteps({ currentItem, scanLocation, scanSku, setScanQty, onPickItem }: UsePickWizardStepsArgs) {
    const [step, setStep] = useState<PickWizardStep>('LOCATION');
    const [localError, setLocalError] = useState('');

    const handleLocationNext = () => {
        if (scanLocation.trim() === currentItem?.locationBarcode?.trim()) {
            setStep('SKU');
            setLocalError('');
        } else {
            setLocalError(`Wrong location! Go to: ${currentItem?.locationBarcode}`);
        }
    };

    const handleSkuNext = () => {
        if (scanSku.trim() === currentItem?.productSku?.trim()) {
            setStep('QUANTITY');
            setLocalError('');
            if (currentItem) {
                setScanQty(currentItem.requiredQuantity - currentItem.pickedQuantity - currentItem.missingQuantity);
            }
        } else {
            setLocalError(`Wrong product! Expected: ${currentItem?.productSku}`);
        }
    };

    const handleConfirm = async () => {
        await onPickItem();
        setStep('LOCATION');
        setLocalError('');
    };

    const goToLocation = () => setStep('LOCATION');
    const goToSku = () => setStep('SKU');

    return { step, localError, handleLocationNext, handleSkuNext, handleConfirm, goToLocation, goToSku };
}
