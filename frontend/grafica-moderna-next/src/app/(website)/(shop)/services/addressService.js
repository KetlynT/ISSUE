import api from '@/app/(website)/services/api';

export const AddressService = {
  getAll: async () => {
    const response = await api.get('/addresses');
    return response.data;
  },

  getById: async (id) => {
    const response = await api.get(`/addresses/${id}`);
    return response.data;
  },

  create: async (addressData) => {
    const response = await api.post('/addresses', addressData);
    return response.data;
  },

  update: async (id, addressData) => {
    await api.put(`/addresses/${id}`, addressData);
  },

  delete: async (id) => {
    await api.delete(`/addresses/${id}`);
  },
};