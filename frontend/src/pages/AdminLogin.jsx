import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Shield, Lock, AlertTriangle } from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminLogin = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { login, logout } = useAuth();

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const data = await login({ email, password });
      
      // ✅ CORREÇÃO: Valida se é Admin ANTES de redirecionar
      if (data.role !== 'Admin') {
        await logout(); // Desloga imediatamente
        toast.error("Acesso restrito a administradores.", { 
          icon: <AlertTriangle className="text-red-500"/> 
        });
        setLoading(false);
        return;
      }
      
      toast.success("Bem-vindo ao Painel!");
      navigate('/putiroski/dashboard');
    } catch (err) {
      toast.error('Credenciais inválidas.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-gray-800 rounded-xl shadow-2xl overflow-hidden border border-gray-700">
        <div className="bg-gray-800 p-8 text-center border-b border-gray-700">
          <div className="w-16 h-16 bg-primary rounded-full flex items-center justify-center mx-auto mb-4 shadow-lg shadow-primary/50">
            <Shield size={32} className="text-white" />
          </div>
          <h2 className="text-2xl font-bold text-white tracking-wide">Acesso Restrito</h2>
          <p className="text-gray-400 text-sm mt-1">Área Administrativa</p>
        </div>

        <form onSubmit={handleLogin} className="p-8 space-y-6">
          <div>
            <label className="block text-gray-400 text-xs font-bold uppercase mb-2">Login Administrativo</label>
            <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Shield size={18} className="text-gray-500" />
                </div>
                <input 
                  type="email" 
                  className="w-full bg-gray-700 text-white border border-gray-600 rounded-lg pl-10 p-3 focus:ring-2 focus:ring-primary focus:border-transparent outline-none transition-all placeholder-gray-500"
                  placeholder="admin@graficamoderna.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
            </div>
          </div>

          <div>
            <label className="block text-gray-400 text-xs font-bold uppercase mb-2">Senha de Segurança</label>
            <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Lock size={18} className="text-gray-500" />
                </div>
                <input 
                  type="password" 
                  className="w-full bg-gray-700 text-white border border-gray-600 rounded-lg pl-10 p-3 focus:ring-2 focus:ring-primary focus:border-transparent outline-none transition-all placeholder-gray-500"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
            </div>
          </div>

          <button 
            type="submit"
            disabled={loading}
            className="w-full bg-primary hover:brightness-110 text-white font-bold py-3 rounded-lg shadow-lg transform transition hover:scale-[1.02] disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? 'Validando...' : 'Acessar Painel'}
          </button>
        </form>

        <div className="bg-gray-900/50 p-4 text-center border-t border-gray-700">
            <p className="text-xs text-gray-500 flex items-center justify-center gap-2">
                <Lock size={12} /> Conexão Segura e Monitorada
            </p>
        </div>
      </div>
    </div>
  );
};