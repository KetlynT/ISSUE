import api from './api';

export const PaymentService = {
  // Chama o backend para gerar a Sessão de Checkout do Stripe
  createCheckoutSession: async (orderId) => {
    try {
      // O backend retorna { url: "https://checkout.stripe.com/..." }
      const response = await api.post(`/payments/checkout/${orderId}`);
      return response.data;
    } catch (error) {
      console.error("Erro no pagamento:", error);
      throw error;
    }
  }
};