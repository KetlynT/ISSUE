import api from './api';

const TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';

const authService = {
  login: async (credentials, isAdmin = false) => {
    const url = isAdmin ? '/auth/admin/login' : '/auth/login';
    const response = await api.post(url, credentials);
    const data = response.data || {};
    if (data.token) {
      try { localStorage.setItem(TOKEN_KEY, data.token); 
        if(data.refreshToken) localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
      } catch {}
    }
    return data;
  },

  register: async (data) => {
    const response = await api.post('/auth/register', data);
    const result = response.data || {};
    if (result.token) {
      try { localStorage.setItem(TOKEN_KEY, result.token); 
        if(result.refreshToken) localStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
      } catch {}
    }
    return result;
  },

  logout: async () => {
    try {
      await api.post('/auth/logout');
    } catch (e) {
    }
    try { localStorage.removeItem(TOKEN_KEY); } catch {}
  },

  getProfile: async () => {
    const response = await api.get('/auth/profile');
    return response.data;
  },

  updateProfile: async (data) => {
    const response = await api.put('/auth/profile', data);
    return response.data;
  },

  checkAuth: async () => {
    const token = (() => { try { return localStorage.getItem(TOKEN_KEY); } catch { return null; } })();
    if (!token) return { isAuthenticated: false, role: null };

    try {
      const response = await api.get('/auth/check-auth');
      return response.data; 
    } catch (error) {
      try { localStorage.removeItem(TOKEN_KEY); } catch {}
      return { isAuthenticated: false, role: null };
    }
  },

  isAuthenticated: () => {
    try {
      return !!localStorage.getItem(TOKEN_KEY);
    } catch {
      return false;
    }
  },

  confirmEmail: async (userId, token) => {
    return await api.post('/auth/confirm-email', { userId, token });
  },

  forgotPassword: async (email) => {
    return await api.post('/auth/forgot-password', { email });
  },

  resetPassword: async (data) => {
    return await api.post('/auth/reset-password', data);
  },

  refreshToken: async () => {
    const accessToken = localStorage.getItem(TOKEN_KEY);
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);

    if(!accessToken || !refreshToken) throw new Error("Tokens não encontrados");

    const response = await api.post('/auth/refresh-token', { accessToken, refreshToken });
    const data = response.data;
    
    if (data.token) {
        localStorage.setItem(TOKEN_KEY, data.token);
        if(data.refreshToken) localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
        return data.token;
    }
    throw new Error("Falha ao atualizar token");
  },

};

export default authService;