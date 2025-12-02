import api from './api';

export const AuthService = {
  login: async (email, password) => {
    try {
      // O backend define o cookie automaticamente aqui
      const response = await api.post('/auth/login', { email, password });
      
      // Salvamos apenas dados úteis para a UI (nome, role), nada sensível
      if (response.data) {
        localStorage.setItem('user', JSON.stringify(response.data));
      }
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  register: async (fullName, email, password) => {
    try {
      const response = await api.post('/auth/register', { fullName, email, password });
      if (response.data) {
        localStorage.setItem('user', JSON.stringify(response.data));
      }
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  logout: async () => {
    try {
        await api.post('/auth/logout'); // Backend apaga o cookie
    } finally {
        localStorage.removeItem('user');
        window.location.href = '/login';
    }
  },

  isAuthenticated: () => {
    // Verificação de UI apenas. A segurança real é feita no backend validando o Cookie.
    const user = localStorage.getItem('user');
    return !!user;
  },

  // Método getToken removido, pois o JS não tem mais acesso ao token
  getUser: () => JSON.parse(localStorage.getItem('user') || '{}')
};