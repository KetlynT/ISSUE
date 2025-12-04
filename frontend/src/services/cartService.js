import api from './api';

export const CartService = {
  // --- Cliente ---
  getCart: async () => {
    const response = await api.get('/cart');
    return response.data;
  },

  addItem: async (productId, quantity) => {
    await api.post('/cart/items', { productId, quantity });
  },

  updateQuantity: async (itemId, quantity) => {
    await api.patch(`/cart/items/${itemId}`, { quantity });
  },

  removeItem: async (itemId) => {
    await api.delete(`/cart/items/${itemId}`);
  },

  clearCart: async () => {
    await api.delete('/cart');
  },

  checkout: async (checkoutData) => {
    const response = await api.post('/cart/checkout', checkoutData);
    return response.data;
  },
  
  getMyOrders: async () => {
    const response = await api.get('/orders');
    return response.data;
  },

  payOrder: async (orderId) => {
    const response = await api.post(`/payment/pay/${orderId}`);
    return response.data;
  },

  requestRefund: async (orderId) => {
    const response = await api.post(`/orders/${orderId}/request-refund`);
    return response.data;
  },

  // --- Admin ---
  getAllOrders: async () => {
    const response = await api.get('/orders/admin/all');
    return response.data;
  },

  // ATUALIZADO: Agora aceita um objeto DTO completo
  updateOrderStatus: async (orderId, updateData) => {
    // updateData espera: { status, trackingCode, reverseLogisticsCode, returnInstructions }
    await api.patch(`/orders/${orderId}/status`, updateData);
  }
};