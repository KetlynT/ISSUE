import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { UserPlus, User, Mail, Lock, Phone } from 'lucide-react';
import toast from 'react-hot-toast';

export const Register = () => {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: ''
  });
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { register } = useAuth(); // Contexto

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    
    if (formData.password !== formData.confirmPassword) {
      toast.error("As senhas não conferem.");
      return;
    }
    if (formData.password.length < 8) { // Ajustado para política mais forte
      toast.error("A senha deve ter pelo menos 8 caracteres.");
      return;
    }
    if (!formData.phoneNumber) {
      toast.error("O telefone é obrigatório.");
      return;
    }

    setLoading(true);
    
    try {
      // Envia objeto único, conforme esperado pelo AuthContext -> AuthService
      await register({
        fullName: formData.fullName,
        email: formData.email,
        password: formData.password,
        phoneNumber: formData.phoneNumber
      });
      
      toast.success("Conta criada com sucesso!");
      navigate('/'); 
    } catch (err) {
      console.error(err);
      // Extrai mensagem de erro do backend (ex: "Senha deve ter caractere especial")
      const msg = err.response?.data?.message || "Erro ao criar conta. Verifique os dados.";
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4 py-12">
      <div className="max-w-md w-full bg-white rounded-xl shadow-lg border border-gray-100 p-8">
        <div className="text-center mb-8">
            <div className="bg-blue-100 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                <UserPlus size={32} className="text-blue-600" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900">Crie sua Conta</h2>
            <p className="text-gray-500 text-sm">Junte-se a nós para gerenciar seus pedidos.</p>
        </div>

        <form onSubmit={handleRegister} className="space-y-4">
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Nome Completo</label>
            <div className="relative">
                <User size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="fullName"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Seu nome"
                    value={formData.fullName}
                    onChange={handleChange}
                    required
                />
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">E-mail</label>
            <div className="relative">
                <Mail size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="email"
                    type="email"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="seu@email.com"
                    value={formData.email}
                    onChange={handleChange}
                    required
                />
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Telefone / WhatsApp</label>
            <div className="relative">
                <Phone size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="phoneNumber"
                    type="tel"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="(11) 99999-9999"
                    value={formData.phoneNumber}
                    onChange={handleChange}
                    required
                />
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Senha</label>
            <div className="relative">
                <Lock size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="password"
                    type="password"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Mínimo 8 caracteres, maiúscula e especial"
                    value={formData.password}
                    onChange={handleChange}
                    required
                />
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Confirmar Senha</label>
            <div className="relative">
                <Lock size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="confirmPassword"
                    type="password"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Repita a senha"
                    value={formData.confirmPassword}
                    onChange={handleChange}
                    required
                />
            </div>
          </div>

          <button 
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 rounded-lg transition-colors flex justify-center mt-6 shadow-lg shadow-blue-500/30 disabled:opacity-70"
          >
            {loading ? 'Criando conta...' : 'Cadastrar'}
          </button>
        </form>

        <div className="mt-6 text-center pt-6 border-t border-gray-100">
            <p className="text-sm text-gray-600">
                Já tem uma conta?{' '}
                <Link to="/login" className="text-blue-600 font-bold hover:underline">
                    Fazer Login
                </Link>
            </p>
        </div>
      </div>
    </div>
  );
};