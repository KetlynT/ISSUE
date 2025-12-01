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

  removeItem: async (itemId) => {
    await api.delete(`/cart/items/${itemId}`);
  },

  clearCart: async () => {
    await api.delete('/cart');
  },

  // --- Cliente: Checkout & Pedidos ---
  checkout: async (checkoutData) => {
    // checkoutData espera: { address, zipCode, couponCode? }
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

  // --- Admin: Gestão de Pedidos ---
  getAllOrders: async () => {
    const response = await api.get('/orders/admin/all');
    return response.data;
  },

  updateOrderStatus: async (orderId, newStatus) => {
    // O backend espera uma string crua no body, por isso o JSON.stringify e o header manual
    await api.patch(`/orders/${orderId}/status`, JSON.stringify(newStatus), {
        headers: { 'Content-Type': 'application/json' }
    });
  }
};