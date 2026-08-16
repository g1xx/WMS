import { useCallback, useState } from 'react';

// Shared by every supervisor-gated submenu (picking's missing-item and defect
// write-offs, putaway's missing-item write-off): badge input, in-flight state,
// and the "empty badge" client-side validation were previously three
// independently drifting copies of the same logic — this is the one implementation.
//
// open/close/submit are wrapped in useCallback so consumers (e.g. a useEffect that
// only needs `close`) get a stable function reference across renders instead of a
// new closure every time — without that, adding them to a dependency array would
// make the effect re-run on every render rather than only when it actually matters.
export function useSupervisorOverride() {
    const [isOpen, setIsOpen] = useState(false);
    const [badge, setBadge] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    const open = useCallback(() => setIsOpen(true), []);

    const close = useCallback(() => {
        setIsOpen(false);
        setBadge('');
    }, []);

    // Validates the badge, runs `action` with isSubmitting toggled around it, and
    // closes the submenu once it settles. `action` owns the actual API call and
    // whatever additional cleanup its own success (or swallowed failure) implies.
    const submit = useCallback(async (action: (badge: string) => Promise<void>) => {
        if (!badge.trim()) {
            alert("Scan the supervisor's badge!");
            return;
        }

        setIsSubmitting(true);
        try {
            await action(badge);
            close();
        } finally {
            setIsSubmitting(false);
        }
    }, [badge, close]);

    return { isOpen, open, close, badge, setBadge, isSubmitting, submit };
}
