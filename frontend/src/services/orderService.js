import api from './api';

export const OrderService = {
  // --- Cliente ---
  checkout: async (checkoutData) => {
    const response = await api.post('/orders', checkoutData);
    return response.data;
  },

  getMyOrders: async (page = 1, pageSize = 10) => {
    const response = await api.get(`/orders?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  requestRefund: async (orderId, refundData) => {
    const response = await api.post(`/orders/${orderId}/request-refund`, refundData);
    return response.data;
  },
};