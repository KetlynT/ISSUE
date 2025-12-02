import api from './api';

export const CartService = {
  // --- Cliente: Carrinho ---
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

  // --- Cliente: Checkout & Pedidos ---
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

  // NOVO: Solicitar Reembolso
  requestRefund: async (orderId) => {
    const response = await api.post(`/orders/${orderId}/request-refund`);
    return response.data;
  },

  // --- Admin: Gestão de Pedidos ---
  getAllOrders: async () => {
    const response = await api.get('/orders/admin/all');
    return response.data;
  },

  updateOrderStatus: async (orderId, newStatus, trackingCode = null) => {
    await api.patch(`/orders/${orderId}/status`, { status: newStatus, trackingCode });
  }
};