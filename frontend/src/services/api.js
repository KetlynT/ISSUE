import axios from 'axios';
import toast from 'react-hot-toast';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5150/api', 
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true 
});

let isRedirecting = false;

const nuclearReset = (destination) => {
    if (isRedirecting) return new Promise(() => {});
    
    if (window.location.pathname === destination) {
        return new Promise(() => {});
    }

    isRedirecting = true;
    console.clear();
    console.warn(`Redirecionando para ${destination} devido a erro crítico.`);

    // 1. SALVA O CONSENTIMENTO ANTES DE LIMPAR
    const savedConsent = localStorage.getItem('lgpd_consent');

    // 2. Limpa tudo
    localStorage.clear();
    sessionStorage.clear();

    // 3. RESTAURA O CONSENTIMENTO
    if (savedConsent) {
        localStorage.setItem('lgpd_consent', savedConsent);
    }

    // Redirecionamento
    window.location.href = destination;
    return new Promise(() => {});
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config || {};
    const url = originalRequest.url || '';

    // --- CENÁRIO 1: BACKEND OFF (Network Error) ---
    if (error.code === "ERR_NETWORK" || !error.response) {
        if (window.location.pathname === '/error') return new Promise(() => {});

        if (url.includes('/login') || url.includes('/register') || window.location.pathname === '/login') {
             toast.error("Servidor indisponível. Tente novamente.");
             return Promise.reject(error);
        }

        return nuclearReset('/error');
    }

    const status = error.response.status;

    // --- CENÁRIO 2: SESSÃO INVÁLIDA (401/403) ---
    if (status === 401 || status === 403) {
        return nuclearReset('/login');
    }

    // --- CENÁRIO 3: ERRO CRÍTICO NO PERFIL (500) ---
    if (status === 500 && (url.includes('/auth/profile') || url.includes('/user'))) {
        return nuclearReset('/login');
    }
    
    return Promise.reject(error);
  }
);

export default api;