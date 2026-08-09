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

const axiosClient = axios.create({
    baseURL: 'http://localhost:5124/api',
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

// Exchanges a supervisor's badge for a short-lived, elevated JWT authorized for a single
// Brigadier/Admin-gated action. Returns an axios request config carrying that token in its
// Authorization header (and skipAuthRedirect, so a bad badge doesn't log the worker out) —
// pass it as the `config` of the one call that needs it. Nothing is persisted, so the
// elevated token is naturally discarded once that request completes.
export async function fetchSupervisorAuthHeader(badgeBarcode: string) {
    const response = await axiosClient.post(
        '/Auth/supervisor-override',
        { badgeBarcode },
        { skipAuthRedirect: true }
    );

    const supervisorToken = response.data.token as string;

    return {
        headers: { Authorization: `Bearer ${supervisorToken}` },
        skipAuthRedirect: true,
    };
}

// True when an error came back as 401/403 — used to tell "the supervisor
// override was rejected" apart from an ordinary business-logic failure.
export function isSupervisorAuthError(error: any): boolean {
    const status = error?.response?.status;
    return status === 401 || status === 403;
}

export default axiosClient;
