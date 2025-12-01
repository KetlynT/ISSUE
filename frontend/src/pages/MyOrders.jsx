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

  removeItem: async (itemId) => {
    await api.delete(`/cart/items/${itemId}`);
  },

  clearCart: async () => {
    await api.delete('/cart');
  },

  checkout: async (addressData) => {
    const response = await api.post('/cart/checkout', addressData);
    return response.data;
  },
  
  getMyOrders: async () => {
    const response = await api.get('/orders');
    return response.data;
  },

  // Novo método para simular pagamento
  payOrder: async (orderId) => {
    const response = await api.post(`/payment/pay/${orderId}`);
    return response.data;
  },

  // --- Admin ---
  getAllOrders: async () => {
    const response = await api.get('/orders/admin/all');
    return response.data;
  },

  updateOrderStatus: async (orderId, newStatus) => {
    await api.patch(`/orders/${orderId}/status`, JSON.stringify(newStatus), {
        headers: { 'Content-Type': 'application/json' }
    });
  }
};