import { QueryClient } from '@tanstack/react-query';

// This runs on dedicated warehouse terminals, not a browser tab a user tabs away
// from and back to — window-focus refetching would just be noise. Retries are off
// so a failed request surfaces immediately as an error state instead of silently
// hammering the API a few times first.
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: false,
            refetchOnWindowFocus: false,
        },
    },
});
