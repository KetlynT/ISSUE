import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api', // Ajuste se a porta for diferente
  withCredentials: true, // OBRIGATÓRIO: Permite o envio de Cookies (JWT)
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor de Resposta para lidar com erros globais
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Se receber 401 (Não autorizado), significa que o cookie expirou ou é inválido
    if (error.response && error.response.status === 401) {
      // Opcional: Redirecionar para login ou limpar estado (será tratado no AuthContext)
      console.warn('Sessão expirada ou não autenticada.');
    }
    return Promise.reject(error);
  }
);

export default api;