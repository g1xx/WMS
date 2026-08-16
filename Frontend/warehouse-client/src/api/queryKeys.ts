// Centralized TanStack Query key factory — keeps the query that populates a
// cache entry and every mutation that reads/invalidates/writes it referencing
// the exact same key, instead of each file retyping its own literal array.
export const queryKeys = {
    pickTask: {
        current: (sector: string) => ['pickTask', 'current', sector] as const,
    },
    putawayTask: {
        active: ['putawayTask', 'active'] as const,
    },
};
