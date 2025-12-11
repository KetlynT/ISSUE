import api from '../modules/api/api';

export const CouponService = {
  validate: async (code) => {
    try {
      const response = await api.get(`/coupons/validate/${code}`);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data || "Cupom inválido");
    }
  },

  getAll: async () => {
    const response = await api.get('/coupons');
    return response.data;
  },

  create: async (couponData) => {
    const response = await api.post('/coupons', couponData);
    return response.data;
  },

  delete: async (id) => {
    await api.delete(`/coupons/${id}`);
  }
};