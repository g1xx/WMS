import axios from 'axios';

export const SECTOR_STORAGE_KEY = 'wms_active_sector';

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
    const token = localStorage.getItem('token');
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

export default axiosClient;