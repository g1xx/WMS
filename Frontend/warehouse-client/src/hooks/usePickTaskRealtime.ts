import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

const HUB_URL = '/hubs/picktasks';

// Push-driven alternative to polling: the "no tasks available" screen used to
// need the worker to hit "Check again" themselves. The backend notifies this
// hub's sector group whenever a task actually becomes claimable there (order
// allocation, a cancelled/leftover task rolling back into the queue, or a
// replacement pick routed to another zone) — see PickTaskHub / IPickTaskNotifier
// on the API side. This only triggers a refetch; it never trusts the socket as
// the source of truth, so a dropped connection just means the worker is back to
// polling manually, not stuck.
//
// onChanged goes through a ref instead of the effect's dependency array on
// purpose — callers pass an inline closure (e.g. wrapping refetchTask), and
// reconnecting the socket every time that closure's identity changes would
// mean rejoining the group on every render instead of once per sector.
export function usePickTaskRealtime(sector: string, onChanged: () => void) {
    const onChangedRef = useRef(onChanged);
    onChangedRef.current = onChanged;

    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                accessTokenFactory: () => localStorage.getItem('token') ?? '',
            })
            .withAutomaticReconnect()
            .build();

        connection.on('PickTasksChanged', () => onChangedRef.current());

        // onreconnected re-joins the group — a reconnect gets a fresh
        // ConnectionId server-side, so the old group membership is gone with it.
        connection.onreconnected(() => {
            void connection.invoke('JoinSector', sector).catch(() => {});
        });

        connection
            .start()
            .then(() => connection.invoke('JoinSector', sector))
            .catch((error) => console.error('PickTask hub connection failed:', error));

        return () => {
            // Best-effort: nothing to leave cleanly if the connection never came
            // up or already dropped, and stop() below tears it down either way.
            connection.invoke('LeaveSector', sector).catch(() => {});
            void connection.stop();
        };
    }, [sector]);
}
