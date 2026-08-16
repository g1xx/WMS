import axios from 'axios';

export const SECTOR_STORAGE_KEY = 'wms_active_sector';

// Lets a single request opt out of the global 401 -> logout behavior below.
// Used for the supervisor-override flow, where a failed elevation must not
// tear down the worker's own session.
declare module 'axios' {
    export interface AxiosRequestConfig {
        skipAuthRedirect?: boolean;
    }
}

// Relative, not an absolute host:port — the browser always calls the API on the
// same origin it loaded the page from. In Docker, nginx proxies /api to the
// backend container (see nginx.conf); in local dev, Vite's dev server does the
// same (see vite.config.ts's server.proxy). This is what makes the app work from
// a remote host at all — a hardcoded "localhost:5124" would only ever resolve to
// whatever machine the browser itself is running on, never the actual server.
const axiosClient = axios.create({
    baseURL: '/api',
});

// The only place session state gets torn down — explicit user logout and a
// 401 (invalid/expired session) both count as "logout": in both cases the
// saved sector must not silently carry over into whatever session comes next.
export function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem(SECTOR_STORAGE_KEY);
    window.location.href = '/login';
}

axiosClient.interceptors.request.use((config) => {
    // Don't clobber a caller-supplied Authorization header (e.g. a short-lived
    // supervisor-override token attached to a single elevated request).
    if (!config.headers.Authorization) {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
    }
    return config;
});

axiosClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401 && !error.config?.skipAuthRedirect) {
            logout();
        }
        return Promise.reject(error);
    }
);

// Shape returned by both /Auth/login and /Auth/supervisor-override.
export interface TokenResponse {
    token: string;
}

// Exchanges a supervisor's badge for a short-lived, elevated JWT authorized for a single
// Brigadier/Admin-gated action. Returns an axios request config carrying that token in its
// Authorization header (and skipAuthRedirect, so a bad badge doesn't log the worker out) —
// pass it as the `config` of the one call that needs it. Nothing is persisted, so the
// elevated token is naturally discarded once that request completes.
export async function fetchSupervisorAuthHeader(badgeBarcode: string) {
    const response = await axiosClient.post<TokenResponse>(
        '/Auth/supervisor-override',
        { badgeBarcode },
        { skipAuthRedirect: true }
    );

    return {
        headers: { Authorization: `Bearer ${response.data.token}` },
        skipAuthRedirect: true,
    };
}

// True when an error came back as 401/403 — used to tell "the supervisor
// override was rejected" apart from an ordinary business-logic failure.
export function isSupervisorAuthError(error: unknown): boolean {
    const status = (error as { response?: { status?: number } })?.response?.status;
    return status === 401 || status === 403;
}

// Shared by every supervisor-gated mutation's onError handler: shows the specific
// override-failure message and reports whether it applied, so the caller knows
// to skip its own generic fallback alert in that case.
export function alertIfSupervisorAuthError(error: unknown): boolean {
    if (!isSupervisorAuthError(error)) return false;
    alert('Supervisor authorization failed: Invalid badge or missing permissions.');
    return true;
}

// Shared by every mutation's onError handler: the backend returns the business error
// message as a plain string response body (see Warehouse.Api.Common.ResultExtensions),
// so this is the one place that knows how to safely pull it out of an unknown error.
export function extractErrorMessage(error: unknown, fallback: string): string {
    const data = (error as { response?: { data?: unknown } })?.response?.data;
    if (typeof data === 'string' && data.trim()) return data;
    return fallback;
}

export default axiosClient;
