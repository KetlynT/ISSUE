import api from './api';

export const OrderService = {
  // --- Cliente ---
  getMyOrders: async () => {
    const response = await api.get('/orders');
    return response.data;
  },

  requestRefund: async (orderId) => {
    const response = await api.post(`/orders/${orderId}/request-refund`);
    return response.data;
  },

  // --- Admin ---
  getAllOrders: async () => {
    const response = await api.get('/orders/all');
    return response.data;
  },

  updateOrderStatus: async (orderId, updateData) => {
    await api.patch(`/orders/${orderId}/status`, updateData);
  }
};