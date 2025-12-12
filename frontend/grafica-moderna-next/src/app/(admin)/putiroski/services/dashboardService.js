import api from '@/app/(website)/services/api';

export const DashboardService = {
  getStats: async () => {
    const response = await api.get('/dashboard/stats');
    return response.data;
  },

  getOrders: async (page = 1, pageSize = 10) => {
    const response = await api.get(`/admin/orders?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  updateOrderStatus: async (id, statusData) => {
    const response = await api.patch(`/admin/orders/${id}/status`, statusData);
    return response.data;
  },

  uploadImage: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return response.data.url;
  },

  updatePage: async (slug, data) => {
    await api.put(`/content/pages/${slug}`, data);
  },

  createPage: async (data) => {
    const response = await api.post('/content', data);
    return response.data;
  },
  
  saveSettings: async (settingsDict) => {
    await api.post('/content/settings', settingsDict);
  }
};