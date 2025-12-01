import React, { useState, useEffect } from 'react';
import { Button } from '../components/ui/Button';
import { User, MapPin, Phone, Mail, Save } from 'lucide-react';
import api from '../services/api';
import toast from 'react-hot-toast';

export const Profile = () => {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    phoneNumber: '',
    zipCode: '',
    address: '',
    city: '',
    state: ''
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const loadData = async () => {
      try {
        const { data } = await api.get('/auth/profile');
        setFormData({
            fullName: data.fullName || '',
            email: data.email || '', // Readonly
            phoneNumber: data.phoneNumber || '',
            zipCode: data.zipCode || '',
            address: data.address || '',
            city: data.city || '',
            state: data.state || ''
        });
      } catch (e) {
        toast.error("Erro ao carregar perfil.");
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await api.put('/auth/profile', formData);
      toast.success("Perfil atualizado com sucesso!");
    } catch (e) {
      toast.error("Erro ao salvar alterações.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="text-center py-20">Carregando perfil...</div>;

  return (
    <div className="max-w-4xl mx-auto px-4 py-12">
      <div className="flex items-center gap-4 mb-8">
        <div className="bg-blue-100 p-4 rounded-full">
            <User size={32} className="text-blue-600" />
        </div>
        <div>
            <h1 className="text-3xl font-bold text-gray-900">Meu Perfil</h1>
            <p className="text-gray-500">Gerencie suas informações pessoais e de entrega.</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="bg-white p-8 rounded-xl shadow-sm border border-gray-200">
        
        {/* Dados Pessoais */}
        <div className="mb-8">
            <h2 className="text-lg font-bold text-gray-800 mb-4 flex items-center gap-2 pb-2 border-b">
                <User size={18} /> Dados Pessoais
            </h2>
            <div className="grid md:grid-cols-2 gap-4">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Nome Completo</label>
                    <input 
                        className="w-full border border-gray-300 rounded-lg p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                        value={formData.fullName}
                        onChange={e => setFormData({...formData, fullName: e.target.value})}
                        required
                    />
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
                    <div className="relative">
                        <Mail size={18} className="absolute left-3 top-3 text-gray-400" />
                        <input 
                            className="w-full border border-gray-200 bg-gray-50 rounded-lg p-2.5 pl-10 text-gray-500 cursor-not-allowed"
                            value={formData.email}
                            disabled
                        />
                    </div>
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Telefone / WhatsApp</label>
                    <div className="relative">
                        <Phone size={18} className="absolute left-3 top-3 text-gray-400" />
                        <input 
                            className="w-full border border-gray-300 rounded-lg p-2.5 pl-10 outline-none focus:ring-2 focus:ring-blue-500"
                            value={formData.phoneNumber}
                            onChange={e => setFormData({...formData, phoneNumber: e.target.value})}
                            placeholder="(00) 00000-0000"
                        />
                    </div>
                </div>
            </div>
        </div>

        {/* Endereço */}
        <div>
            <h2 className="text-lg font-bold text-gray-800 mb-4 flex items-center gap-2 pb-2 border-b">
                <MapPin size={18} /> Endereço de Entrega Padrão
            </h2>
            <div className="grid md:grid-cols-6 gap-4">
                <div className="md:col-span-2">
                    <label className="block text-sm font-medium text-gray-700 mb-1">CEP</label>
                    <input 
                        className="w-full border border-gray-300 rounded-lg p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                        value={formData.zipCode}
                        onChange={e => setFormData({...formData, zipCode: e.target.value})}
                        placeholder="00000-000"
                    />
                </div>
                <div className="md:col-span-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Cidade</label>
                    <input 
                        className="w-full border border-gray-300 rounded-lg p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                        value={formData.city}
                        onChange={e => setFormData({...formData, city: e.target.value})}
                    />
                </div>
                <div className="md:col-span-5">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Endereço (Rua, Nº, Bairro)</label>
                    <input 
                        className="w-full border border-gray-300 rounded-lg p-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                        value={formData.address}
                        onChange={e => setFormData({...formData, address: e.target.value})}
                    />
                </div>
                <div className="md:col-span-1">
                    <label className="block text-sm font-medium text-gray-700 mb-1">UF</label>
                    <input 
                        className="w-full border border-gray-300 rounded-lg p-2.5 outline-none focus:ring-2 focus:ring-blue-500 uppercase"
                        value={formData.state}
                        maxLength={2}
                        onChange={e => setFormData({...formData, state: e.target.value})}
                    />
                </div>
            </div>
        </div>

        <div className="mt-8 flex justify-end">
            <Button type="submit" isLoading={saving} className="px-8">
                <Save size={18} className="mr-2" /> Salvar Alterações
            </Button>
        </div>

      </form>
    </div>
  );
};