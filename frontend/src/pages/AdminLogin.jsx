import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Lock, ShieldAlert } from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminLogin = () => {
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleLogin = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      // O segundo parâmetro 'true' indica login de Admin
      await login(formData, true);
      toast.success("Bem-vindo, Administrador.");
      navigate('/putiroski/dashboard');
    } catch (error) {
      console.error(error);
      const msg = error.response?.data?.message || "Acesso negado.";
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900 px-4">
      <div className="max-w-sm w-full bg-white rounded-xl shadow-2xl overflow-hidden">
        <div className="bg-red-600 p-6 text-center">
            <div className="bg-white/20 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4 backdrop-blur-sm">
                <ShieldAlert size={32} className="text-white" />
            </div>
            <h2 className="text-2xl font-bold text-white">Área Restrita</h2>
            <p className="text-red-100 text-sm">Acesso exclusivo administrativo</p>
        </div>

        <div className="p-8">
            <form onSubmit={handleLogin} className="space-y-4">
            <div>
                <label className="block text-sm font-bold text-gray-700 mb-1">E-mail Corporativo</label>
                <input 
                    name="email"
                    type="email"
                    className="w-full border border-gray-300 rounded-lg p-2.5 focus:ring-2 focus:ring-red-500 outline-none"
                    value={formData.email}
                    onChange={handleChange}
                    required
                />
            </div>

            <div>
                <label className="block text-sm font-bold text-gray-700 mb-1">Senha de Acesso</label>
                <input 
                    name="password"
                    type="password"
                    className="w-full border border-gray-300 rounded-lg p-2.5 focus:ring-2 focus:ring-red-500 outline-none"
                    value={formData.password}
                    onChange={handleChange}
                    required
                />
            </div>

            <button 
                type="submit"
                disabled={loading}
                className="w-full bg-gray-900 hover:bg-gray-800 text-white font-bold py-3 rounded-lg transition-colors flex justify-center items-center gap-2 mt-4"
            >
                <Lock size={18} />
                {loading ? 'Autenticando...' : 'Acessar Painel'}
            </button>
            </form>
            
            <div className="mt-6 text-center border-t border-gray-100 pt-4">
                <button onClick={() => navigate('/')} className="text-sm text-gray-500 hover:text-gray-800">
                    ← Voltar para a Loja
                </button>
            </div>
        </div>
      </div>
    </div>
  );
};