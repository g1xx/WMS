import axios from 'axios';

// Relative, not an absolute host:port — same reasoning as warehouse-client's
// axiosClient.ts. In Docker, nginx serves this app under /inbound/ and proxies /api
// to the backend container on the same origin (see nginx.conf); in local dev, Vite's
// dev server does the same (see vite.config.ts's server.proxy). A hardcoded
// "localhost:5124" only ever resolves on the machine the browser itself runs on.
const axiosClient = axios.create({
    baseURL: '/api',
});

// A distinct key from warehouse-client's 'token' — both apps are served from the same
// origin (different paths under it), and localStorage is scoped per-origin, not per-path.
// Sharing a key would mean signing into one app silently overwrites the other's session.
const TOKEN_STORAGE_KEY = 'wms_inbound_feed_token';

export function getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
}

export function logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
}

axiosClient.interceptors.request.use((config) => {
    const token = getToken();
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

axiosClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            logout();
        }
        return Promise.reject(error);
    }
);

// Shape returned by /Auth/login.
export interface TokenResponse {
    token: string;
}

export function extractErrorMessage(error: unknown, fallback: string): string {
    const data = (error as { response?: { data?: unknown } })?.response?.data;
    if (typeof data === 'string' && data.trim()) return data;
    if (data && typeof data === 'object' && 'message' in data) {
        const message = (data as { message?: unknown }).message;
        if (typeof message === 'string' && message.trim()) return message;
    }
    return fallback;
}

export default axiosClient;
