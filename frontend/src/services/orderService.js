import api from './api';

export const OrderService = {
  // --- Cliente ---
  checkout: async (checkoutData) => {
    const response = await api.post('/cart/checkout', checkoutData);
    return response.data;
  },
  
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
    const response = await api.get('/admin/orders');
    return response.data;
  },

  updateOrderStatus: async (orderId, updateData) => {
    await api.patch(`/admin/${orderId}/status`, updateData);
  }
};