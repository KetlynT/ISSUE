import api from './api';

const TOKEN_KEY = 'access_token';

const authService = {
  login: async (credentials, isAdmin = false) => {
    const url = isAdmin ? '/auth/admin/login' : '/auth/login';
    const response = await api.post(url, credentials);
    const data = response.data || {};
    if (data.token) {
      try { localStorage.setItem(TOKEN_KEY, data.token); } catch {}
    }
    return data;
  },

  register: async (data) => {
    const response = await api.post('/auth/register', data);
    const result = response.data || {};
    if (result.token) {
      try { localStorage.setItem(TOKEN_KEY, result.token); } catch {}
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
  }
};

export default authService;