import api from '@/app/(website)/services/api';

export const ContentService = {
  getPage: async (slug) => {
    try {
      const response = await api.get(`/content/pages/${slug}`);
      return response.data;
    } catch (error) {
      return null;
    }
  },

  getAllPages: async () => {
    const response = await api.get('/content/pages');
    return response.data;
  },

  getSettings: async () => {
    try {
      const response = await api.get('/content/settings');
      return response.data;
    } catch (error) {
      console.error("Erro ao carregar configurações", error);
      throw error; 
    }
  },

};