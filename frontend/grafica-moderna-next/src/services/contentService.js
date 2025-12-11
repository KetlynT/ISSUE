import api from './api';

export const ContentService = {
  getPage: async (slug) => {
    try {
      const response = await api.get(`/content/pages/${slug}`);
      return response.data;
    } catch (error) {
      return null;
    }
  },

  createPage: async (data) => {
    const response = await api.post('/content', data);
    return response.data;
  },

  getAllPages: async () => {
    const response = await api.get('/content/pages');
    return response.data;
  },

  updatePage: async (slug, data) => {
    await api.put(`/content/pages/${slug}`, data);
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

  saveSettings: async (settingsDict) => {
    await api.post('/content/settings', settingsDict);
  }
};