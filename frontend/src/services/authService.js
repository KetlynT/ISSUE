import api from './api';

const authService = {
  login: async (credentials, isAdmin = false) => {
    const url = isAdmin ? '/auth/admin/login' : '/auth/login';
    const response = await api.post(url, credentials);
    return response.data;
  },

  register: async (data) => {
    const response = await api.post('/auth/register', data);
    return response.data;
  },

  logout: async () => {
    try {
      await api.post('/auth/logout');
    } catch (e) {
      console.error(e);
    }
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
    try {
      const response = await api.get('/auth/check-auth');
      return response.data; 
    } catch (error) {
      return { isAuthenticated: false, role: null };
    }
  },

  isAuthenticated: async () => {
      try {
          await api.get('/auth/check-auth');
          return true;
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
};

export default authService;