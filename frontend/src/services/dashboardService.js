import api from './api';

export const DashboardService = {
  getStats: async () => {
    const response = await api.get('/dashboard/stats');
    return response.data;
  },
  getOrders: async (page = 1, pageSize = 10) => {
    const response = await api.get(`/dashboard/orders?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },
  updateOrderStatus: async (id, statusData) => {
    const response = await api.patch(`/dashboard/orders/${id}/status`, statusData);
    return response.data;
  }
};