'use client'

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { UserPlus, User, Mail, Lock, Phone, FileText } from 'lucide-react';
import toast from 'react-hot-toast';
import { maskCpfCnpj, maskPhone, cleanString, validateDocument } from '@/utils/formatters';

export const Register = () => {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    cpfCnpj: '',
    password: '',
    confirmPassword: '',
    phoneNumber: ''
  });
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const { register } = useAuth();

  const handleChange = (e) => {
    const { name, value } = e.target;
    let formattedValue = value;

    if (name === 'cpfCnpj') {
      formattedValue = maskCpfCnpj(value);
    } else if (name === 'phoneNumber') {
      formattedValue = maskPhone(value);
    }

    setFormData({ ...formData, [name]: formattedValue });
  };

  const handleRegister = async (e) => {
    e.preventDefault();

    if (!validateDocument(formData.cpfCnpj)) {
      toast.error("CPF ou CNPJ inválido (verifique os dígitos).");
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      toast.error("As senhas não conferem.");
      return;
    }

    if (formData.password.length < 8) {
      toast.error("A senha deve ter pelo menos 8 caracteres.");
      return;
    }

    if (!formData.phoneNumber) {
      toast.error("O telefone é obrigatório.");
      return;
    }

    setLoading(true);
    try {
      await register({
        fullName: formData.fullName,
        cpfCnpj: cleanString(formData.cpfCnpj),
        phoneNumber: cleanString(formData.phoneNumber),
        email: formData.email,
        password: formData.password
      });
      toast.success("Conta criada com sucesso!");
      router.replace('/'); 
    } catch (err) {
      console.error(err);
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
            <div className="bg-primary/10 w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                <UserPlus size={32} className="text-primary" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900">Crie sua Conta</h2>
            <p className="text-gray-500 text-sm">Junte-se a nós para gerenciar seus pedidos.</p>
        </div>

        <form onSubmit={handleRegister} className="space-y-4">
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">CPF ou CNPJ</label>
            <div className="relative">
                <FileText size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="cpfCnpj"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
                    placeholder="000.000.000-00"
                    value={formData.cpfCnpj}
                    onChange={handleChange}
                    maxLength={18}
                    required
                />
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Nome Completo</label>
            <div className="relative">
                <User size={18} className="absolute left-3 top-3 text-gray-400" />
                <input 
                    name="fullName"
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
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
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
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
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
                    placeholder="(11) 99999-9999"
                    value={formData.phoneNumber}
                    onChange={handleChange}
                    maxLength={15}
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
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
                    placeholder="Mínimo 8 caracteres"
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
                    className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary"
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
            className="w-full bg-primary hover:brightness-90 text-white font-bold py-3 rounded-lg transition-colors flex justify-center mt-6 shadow-lg shadow-primary/30 disabled:opacity-70"
          >
            {loading ? 'Criando conta...' : 'Cadastrar'}
          </button>
        </form>

        <div className="mt-6 text-center pt-6 border-t border-gray-100">
            <p className="text-sm text-gray-600">
                Já tem uma conta?{' '}
                <Link href="/login" className="text-primary font-bold hover:underline">
                    Fazer Login
                </Link>
            </p>
        </div>
      </div>
    </div>
  );
};