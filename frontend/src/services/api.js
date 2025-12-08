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
  (error) => {
    if (error.response && error.response.status === 401) {
      const token = localStorage.getItem('access_token');
      // Só limpa e redireciona se já existia um token (sessão expirou)
      if (token) {
        localStorage.removeItem('access_token');
        const isAuthRoute = error.config.url.includes('/login') || error.config.url.includes('/auth');
        if (!isAuthRoute) {
            // Opcional: Redirecionar para login ou apenas limpar estado
            window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);

export default api;