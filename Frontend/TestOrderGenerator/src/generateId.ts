// crypto.randomUUID() only exists in a secure context (HTTPS, or localhost — which is
// why this never surfaced testing the generator standalone). The deployed site is
// plain http:// on a real domain, so the API is simply absent there and calling it
// throws instead of degrading. These IDs are only ever used as React list keys for
// demo UI state, not anything security-sensitive, so a Math.random()-based fallback
// is fine — cryptographic quality isn't required here.
export function generateId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
        return crypto.randomUUID();
    }
    return `id-${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}
