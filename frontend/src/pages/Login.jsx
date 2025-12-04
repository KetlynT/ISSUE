import React, { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom'; 
import { useAuth } from '../context/AuthContext'; // NOVO HOOK
import { useCart } from '../context/CartContext';
import { LogIn, AlertCircle } from 'lucide-react';
import toast from 'react-hot-toast';

export const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth(); // Função do Contexto
  const { syncGuestCart } = useCart();

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    
    try {
      // O Contexto lida com a chamada API e atualização de estado
      const data = await login({ email, password });
      
      if (data.role === 'Admin') {
          // Se for admin tentando logar na área de cliente, o App.jsx vai redirecionar
          // ou podemos barrar aqui. O ideal é deixar o redirecionamento natural.
          // Mas mantendo a lógica original de aviso:
          // Nota: O login já aconteceu no context. Se quisermos bloquear, teríamos que dar logout.
          // Vamos deixar passar, o PublicOnlyRoute/PrivateRoute cuida do resto.
          toast("Bem-vindo, Administrador!", { icon: '🛡️' });
      } else {
          await syncGuestCart();
      }
      
      const from = location.state?.from?.pathname || '/';
      navigate(from, { replace: true });
      
    } catch (err) {
      console.error(err);
      toast.error('Email ou senha incorretos.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
      <div className="max-w-md w-full bg-white rounded-xl shadow-lg border border-gray-100 p-8">
        <div className="text-center mb-8">
            <div className="bg-blue-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                <LogIn size={32} className="text-blue-600" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900">Área do Cliente</h2>
            <p className="text-gray-500 text-sm">Bem-vindo de volta!</p>
        </div>

        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="block text-gray-700 text-sm font-bold mb-1">Email</label>
            <input 
              type="email" 
              className="w-full border border-gray-300 p-3 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-all"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="block text-gray-700 text-sm font-bold mb-1">Senha</label>
            <input 
              type="password" 
              className="w-full border border-gray-300 p-3 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-all"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button 
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:opacity-70 text-white font-bold py-3 rounded-lg transition-colors flex justify-center shadow-lg shadow-blue-500/30"
          >
            {loading ? 'Entrando...' : 'Entrar'}
          </button>
        </form>

        <div className="mt-6 text-center pt-6 border-t border-gray-100 space-y-2">
            <p className="text-sm text-gray-600">
                Não tem uma conta?{' '}
                <Link to="/cadastro" className="text-blue-600 font-bold hover:underline">
                    Cadastre-se
                </Link>
            </p>
            <div>
                <a href="/" className="text-xs text-gray-400 hover:text-gray-600">← Voltar para a loja</a>
            </div>
        </div>
      </div>
    </div>
  );
};