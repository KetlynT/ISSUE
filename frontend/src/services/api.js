import axios from 'axios';

const api = axios.create({
  baseURL: 'https://localhost:7255/api', // Somente HTTPS
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
      localStorage.removeItem('access_token');
      const isLoginRequest = error.config.url.includes('/login') || error.config.url.includes('/auth');
      if (!isLoginRequest) {
          window.location.href = '/';
      }
      console.warn('Sessão expirada ou não autenticada.');
    }
    return Promise.reject(error);
  }
);

export default api;