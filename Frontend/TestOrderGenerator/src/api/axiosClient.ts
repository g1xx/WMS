import axios from 'axios';

// Products/Orders endpoints are not behind [Authorize], so no auth token is needed here.
const axiosClient = axios.create({
    baseURL: 'http://localhost:5124/api',
});

export default axiosClient;
