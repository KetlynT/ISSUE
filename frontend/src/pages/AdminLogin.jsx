import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthService } from '../services/authService';
import { Shield, Lock, AlertTriangle } from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminLogin = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    
    try {
      const data = await AuthService.login(email, password);
      
      // VALIDAÇÃO: Apenas Admin pode passar por aqui
      if (data.role !== 'Admin') {
          await AuthService.logout();
          toast.error("Acesso negado. Esta área é restrita.");
          setLoading(false);
          return;
      }

      toast.success("Bem-vindo, Administrador.");
      // Redireciona para o dashboard (que fica 'escondido' após o login)
      navigate('/putiroski/dashboard'); 
      
    } catch (err) {
      toast.error("Credenciais inválidas.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900 px-4">
      <div className="max-w-md w-full bg-gray-800 rounded-2xl shadow-2xl border border-gray-700 p-8">
        <div className="text-center mb-8">
            <div className="bg-red-900/20 w-20 h-20 rounded-full flex items-center justify-center mx-auto mb-4 border border-red-900/50">
                <Shield size={40} className="text-red-500" />
            </div>
            <h2 className="text-2xl font-bold text-white">Acesso Restrito</h2>
            <p className="text-gray-400 text-sm mt-1">Portal Administrativo</p>
        </div>

        <form onSubmit={handleLogin} className="space-y-6">
          <div>
            <label className="block text-gray-400 text-xs font-bold uppercase tracking-wider mb-2">Identificação</label>
            <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500">@</span>
                </div>
                <input 
                  type="email" 
                  className="w-full bg-gray-900 border border-gray-700 text-white p-3 pl-10 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-transparent outline-none transition-all"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  placeholder=""
                />
            </div>
          </div>
          
          <div>
            <label className="block text-gray-400 text-xs font-bold uppercase tracking-wider mb-2">Chave de Acesso</label>
            <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Lock size={18} className="text-gray-500" />
                </div>
                <input 
                  type="password" 
                  className="w-full bg-gray-900 border border-gray-700 text-white p-3 pl-10 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-transparent outline-none transition-all"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  placeholder=""
                />
            </div>
          </div>

          <button 
            type="submit"
            disabled={loading}
            className="w-full bg-gradient-to-r from-red-600 to-red-800 hover:from-red-500 hover:to-red-700 text-white font-bold py-3 rounded-lg transition-all transform active:scale-95 shadow-lg shadow-red-900/20 flex justify-center items-center gap-2"
          >
            {loading ? 'Verificando...' : 'Acessar Painel'}
          </button>
        </form>

        <div className="mt-8 text-center border-t border-gray-700 pt-6">
            <p className="text-xs text-gray-500 flex items-center justify-center gap-2">
                <AlertTriangle size={12} /> Monitoramento de IP ativo
            </p>
        </div>
      </div>
    </div>
  );
};