import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7255/api';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  try {
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers = config.headers || {};
      config.headers['Authorization'] = `Bearer ${token}`;
    }
  } catch (e) {
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      const isAuthRoute = originalRequest.url.includes('/login') || originalRequest.url.includes('/auth/register');
      
      if (isAuthRoute) {
        return Promise.reject(error);
      }

      originalRequest._retry = true;

      try {
        const newToken = await authService.refreshToken();
        api.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;
        originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        await authService.logout();
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);
export default api;