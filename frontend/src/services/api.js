import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api', // Ajuste se a porta for diferente
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: adiciona Authorization header com Bearer token se disponível
api.interceptors.request.use((config) => {
  try {
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers = config.headers || {};
      config.headers['Authorization'] = `Bearer ${token}`;
    }
  } catch (e) {
    // ignore
  }
  return config;
});

// Interceptor de Resposta para lidar com erros globais
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Se receber 401 (Não autorizado), significa que o token expirou ou é inválido
    if (error.response && error.response.status === 401) {
      // Opcional: Redirecionar para login ou limpar estado (será tratado no AuthContext)
      console.warn('Sessão expirada ou não autenticada.');
    }
    return Promise.reject(error);
  }
);

export default api;