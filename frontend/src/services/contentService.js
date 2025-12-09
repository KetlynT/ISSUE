import api from './api';

export const ContentService = {
  // Pega uma página específica pelo slug (Público)
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

  // Pega lista de todas as páginas (Admin)
  getAllPages: async () => {
    const response = await api.get('/content/pages');
    return response.data;
  },

  // Atualiza uma página (Admin)
  updatePage: async (id, data) => {
    await api.put(`/content/pages/${id}`, data);
  },

  // Pega todas as configurações (Público/Admin)
  getSettings: async () => {
    try {
      const response = await api.get('/content/settings');
      return response.data;
    } catch (error) {
      console.error("Erro ao carregar configurações", error);
      throw error; 
    }
},

  // Salva configurações em lote (Admin)
  saveSettings: async (settingsDict) => {
    await api.post('/content/settings', settingsDict);
  }
};