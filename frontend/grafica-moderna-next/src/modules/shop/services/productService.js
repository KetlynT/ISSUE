import api from '../lib/api';

export const ProductService = {
  getAll: async (page = 1, pageSize = 8, search = '', sort = '', order = '') => {
    const params = new URLSearchParams({ page, pageSize });
    if (search) params.append('search', search);
    if (sort) params.append('sort', sort);
    if (order) params.append('order', order);

    const response = await api.get(`/products?${params.toString()}`);
    return response.data;
  },

  getById: async (id) => {
    const response = await api.get(`/products/${id}`);
    return response.data;
  },

  create: async (productData) => {
    const response = await api.post('/products', productData);
    return response.data;
  },

  update: async (id, productData) => {
    await api.put(`/products/${id}`, productData);
  },

  delete: async (id) => {
    await api.delete(`/products/${id}`);
  },
};