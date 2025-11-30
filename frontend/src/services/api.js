import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5150/api', // Ajuste para a porta da sua API .NET
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api;