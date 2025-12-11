import api from '../modules/api/api';

export const PaymentService = {
  createCheckoutSession: async (orderId) => {
    const response = await api.post(`/payments/checkout-session/${orderId}`);
    return response.data;
  },
  getPaymentStatus: async (orderId) => {
    const response = await api.get(`/payments/status/${orderId}`);
    return response.data;
  }
};