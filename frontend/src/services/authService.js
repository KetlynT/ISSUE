import api from './api';

const authService = {
  // Login: O backend define o cookie. Não recebemos token no JSON.
  login: async (credentials) => {
    const response = await api.post('/auth/login', credentials);
    return response.data;
  },

  // Registro: O backend define o cookie automaticamente.
  register: async (data) => {
    const response = await api.post('/auth/register', data);
    return response.data;
  },

  // Logout: Chama o backend para invalidar o cookie (Blacklist).
  logout: async () => {
    await api.post('/auth/logout');
  },

  // Perfil: Busca dados do utilizador logado.
  getProfile: async () => {
    const response = await api.get('/auth/profile');
    return response.data;
  },

  // Update Profile
  updateProfile: async (data) => {
    const response = await api.put('/auth/profile', data);
    return response.data;
  },

  // NOVO: Verifica se o cookie é válido ao carregar a página
  checkAuth: async () => {
    try {
      // Endpoint leve criado no AuthController (Batch 2)
      // Se der 401, cai no catch.
      const response = await api.get('/auth/check-auth'); 
      return response.data; // { isAuthenticated: true, role: "..." }
    } catch (error) {
      return { isAuthenticated: false, role: null };
    }
  }
};

export default authService;