import api from './api';

export const CouponService = {
  // Público
  validate: async (code) => {
    try {
      const response = await api.get(`/coupons/validate/${code}`);
      return response.data; // { code, discountPercentage }
    } catch (error) {
      throw new Error(error.response?.data || "Cupom inválido");
    }
  },

  // Admin
  getAll: async () => {
    const response = await api.get('/coupons');
    return response.data;
  },

  create: async (couponData) => {
    // couponData: { code, discountPercentage, validityDays }
    const response = await api.post('/coupons', couponData);
    return response.data;
  },

  delete: async (id) => {
    await api.delete(`/coupons/${id}`);
  }
};