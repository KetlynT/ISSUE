import api from './api';

export const ShippingService = {
  calculate: async (cep, items) => {
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

  calculateForProduct: async (productId, cep) => {
    const cleanCep = cep.replace(/\D/g, '');
    const response = await api.get(`/shipping/product/${productId}/${cleanCep}`);
    return response.data;
  }
};