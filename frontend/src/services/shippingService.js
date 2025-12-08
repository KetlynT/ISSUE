import api from './api';

export const ShippingService = {
  // Calcula frete para vários itens (Carrinho)
  calculate: async (cep, items) => {
    // Backend espera: { destinationCep: "...", items: [...] }
    const payload = {
      destinationCep: cep,
      items: items.map(item => ({
        productId: item.productId,
        quantity: item.quantity
      }))
    };
    
    const response = await api.post('/shipping/calculate', payload);
    return response.data;
  },

  // Calcula frete para um único produto (Página de Detalhes)
  calculateForProduct: async (productId, cep) => {
    // Backend espera GET /shipping/product/{id}/{cep}
    const cleanCep = cep.replace(/\D/g, '');
    const response = await api.get(`/shipping/product/${productId}/${cleanCep}`);
    return response.data;
  }
};