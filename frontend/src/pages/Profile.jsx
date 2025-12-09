import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import authService from '../services/authService';
import toast from 'react-hot-toast';
import { User, Phone, FileText, Save, Loader } from 'lucide-react';
import { maskCpfCnpj, maskPhone, cleanString } from '../utils/formatters';

export const Profile = () => {
  const { user, logout } = useAuth();
  const [formData, setFormData] = useState({
    fullName: '',
    phoneNumber: '',
    cpfCnpj: '',
    email: ''
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const data = await authService.getProfile();
        setFormData({
            fullName: data.fullName || '',
            // Aplica máscara ao receber os dados brutos do backend
            phoneNumber: maskPhone(data.phoneNumber || ''),
            cpfCnpj: maskCpfCnpj(data.cpfCnpj || ''),
            email: data.email || ''
        });
      } catch (error) {
        toast.error("Erro ao carregar perfil.");
      } finally {
        setLoading(false);
      }
    };
    fetchProfile();
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;
    let formattedValue = value;

    // Aplica máscara em tempo real
    if (name === 'cpfCnpj') {
      formattedValue = maskCpfCnpj(value);
    } else if (name === 'phoneNumber') {
      formattedValue = maskPhone(value);
    }

    setFormData({ ...formData, [name]: formattedValue });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      // Remove formatação antes de enviar para o backend
      await authService.updateProfile({
        fullName: formData.fullName,
        phoneNumber: cleanString(formData.phoneNumber),
        cpfCnpj: cleanString(formData.cpfCnpj)
      });
      toast.success("Perfil atualizado com sucesso!");
    } catch (error) {
      const msg = error.response?.data?.message || "Erro ao atualizar.";
      toast.error(msg);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
        <div className="flex justify-center items-center h-96">
            <Loader className="animate-spin text-primary" size={40} />
        </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-12 max-w-2xl">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">Meu Perfil</h1>
      
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="p-6 sm:p-8">
            <form onSubmit={handleSubmit} className="space-y-6">
                
                {/* Email (Readonly) */}
                <div>
                    <label className="block text-sm font-bold text-gray-700 mb-1">E-mail (não alterável)</label>
                    <input 
                        value={formData.email}
                        readOnly
                        className="w-full bg-gray-50 border border-gray-200 rounded-lg p-3 text-gray-500 cursor-not-allowed"
                    />
                </div>

                {/* Nome */}
                <div>
                    <label className="block text-sm font-bold text-gray-700 mb-1">Nome Completo</label>
                    <div className="relative">
                        <User className="absolute left-3 top-3 text-gray-400" size={18} />
                        <input 
                            name="fullName"
                            value={formData.fullName}
                            onChange={handleChange}
                            className="w-full border border-gray-300 rounded-lg pl-10 p-3 focus:ring-2 focus:ring-primary outline-none"
                            required
                        />
                    </div>
                </div>

                {/* CPF/CNPJ */}
                <div>
                    <label className="block text-sm font-bold text-gray-700 mb-1">CPF ou CNPJ</label>
                    <div className="relative">
                        <FileText className="absolute left-3 top-3 text-gray-400" size={18} />
                        <input 
                            name="cpfCnpj"
                            value={formData.cpfCnpj}
                            onChange={handleChange}
                            className="w-full border border-gray-300 rounded-lg pl-10 p-3 focus:ring-2 focus:ring-primary outline-none"
                            placeholder="000.000.000-00"
                            maxLength={18}
                            required
                        />
                    </div>
                </div>

                {/* Telefone */}
                <div>
                    <label className="block text-sm font-bold text-gray-700 mb-1">Telefone / Celular</label>
                    <div className="relative">
                        <Phone className="absolute left-3 top-3 text-gray-400" size={18} />
                        <input 
                            name="phoneNumber"
                            value={formData.phoneNumber}
                            onChange={handleChange}
                            className="w-full border border-gray-300 rounded-lg pl-10 p-3 focus:ring-2 focus:ring-primary outline-none"
                            placeholder="(00) 00000-0000"
                            maxLength={15}
                            required
                        />
                    </div>
                </div>

                <div className="pt-4 flex gap-4">
                    <button 
                        type="submit"
                        disabled={saving}
                        className="flex-1 bg-primary hover:brightness-90 text-white font-bold py-3 rounded-lg flex justify-center items-center gap-2 shadow-lg shadow-primary/30 transition-all disabled:opacity-70"
                    >
                        {saving ? <Loader className="animate-spin" size={20} /> : <Save size={20} />}
                        {saving ? 'Salvando...' : 'Salvar Alterações'}
                    </button>

                    <button 
                        type="button"
                        onClick={logout}
                        className="px-6 py-3 border border-red-200 text-red-600 font-bold rounded-lg hover:bg-red-50 transition-colors"
                    >
                        Sair da Conta
                    </button>
                </div>
            </form>
        </div>
      </div>
    </div>
  );
};