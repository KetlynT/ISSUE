import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import authService from '../services/authService';
import toast from 'react-hot-toast';
import { Mail, ArrowLeft } from 'lucide-react';

export const ForgotPassword = () => {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await authService.forgotPassword(email);
      toast.success("Se o e-mail estiver cadastrado, enviaremos um link de recuperação.");
      setEmail('');
    } catch (error) {
      toast.error("Erro ao solicitar recuperação.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full bg-white rounded-xl shadow-lg border border-gray-100 p-8">
        <div className="text-center mb-8">
          <div className="bg-primary/10 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
            <Mail size={32} className="text-primary" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900">Recuperar Senha</h2>
          <p className="text-gray-500 text-sm mt-2">
            Digite seu e-mail para receber o link de redefinição.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">E-mail Cadastrado</label>
            <input 
              type="email"
              required
              className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-primary"
              placeholder="seu@email.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <button 
            type="submit" 
            disabled={loading}
            className="w-full bg-primary hover:brightness-90 text-white font-bold py-3 rounded-lg transition-colors shadow-lg shadow-primary/30 disabled:opacity-70"
          >
            {loading ? 'Enviando...' : 'Enviar Link'}
          </button>
        </form>

        <div className="mt-6 text-center">
          <Link to="/login" className="text-gray-600 hover:text-primary flex items-center justify-center gap-2 text-sm font-medium">
            <ArrowLeft size={16} /> Voltar para o Login
          </Link>
        </div>
      </div>
    </div>
  );
};