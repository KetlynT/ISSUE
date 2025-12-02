import axios from 'axios';

// Configura o Axios para se comunicar com o backend
const api = axios.create({
  // Garante que usa a URL do ambiente ou o localhost padrão
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5150/api', 
  headers: {
    'Content-Type': 'application/json',
  },
  // ESSENCIAL: Permite que o cookie de segurança (HttpOnly) seja enviado e recebido.
  // Sem isso, o login seguro não funciona.
  withCredentials: true 
});

// Interceptor para tratar erros globais (como sessão expirada)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Se der erro 401 (Não autorizado), significa que o cookie venceu ou é inválido
    if (error.response && error.response.status === 401) {
      // Evita loop infinito se já estiver na página de login
      if (!window.location.pathname.includes('/login')) {
          localStorage.removeItem('user'); // Limpa dados visuais do usuário
          window.location.href = '/login'; // Redireciona
      }
    }
    return Promise.reject(error);
  }
);

export default api;