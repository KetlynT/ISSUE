import React, { useState, useEffect } from 'react';
import { User, Phone, Mail } from 'lucide-react';
import api from '../services/api';
import { AddressManager } from '../components/AddressManager';
import toast from 'react-hot-toast';

export const Profile = () => {
  const [userData, setUserData] = useState({ fullName: '', email: '' });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/auth/profile')
      .then(res => setUserData(res.data))
      .catch(() => toast.error("Erro ao carregar perfil."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="text-center py-20">Carregando...</div>;

  return (
    <div className="max-w-5xl mx-auto px-4 py-12">
      <h1 className="text-3xl font-bold text-gray-900 mb-8 flex items-center gap-3">
        <User className="text-primary" /> Meu Perfil
      </h1>

      <div className="grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-1 h-fit bg-white p-6 rounded-xl shadow-sm border border-gray-100">
          <div className="flex flex-col items-center text-center mb-6">
            {/* Avatar com cor primária */}
            <div className="w-20 h-20 bg-primary/10 rounded-full flex items-center justify-center text-primary font-bold text-3xl mb-3">
              {userData.fullName?.charAt(0) || 'U'}
            </div>
            <h2 className="text-xl font-bold text-gray-800">{userData.fullName}</h2>
            <p className="text-sm text-gray-500 mb-4 flex items-center gap-2 justify-center"><Mail size={14}/> {userData.email}</p>
            
            <p className="text-sm text-gray-500 mb-2 flex items-center gap-2 justify-center">
              <FileText size={14}/> {userData.cpfCnpj || 'CPF não informado'}
            </p>

            <div className="w-full pt-4 border-t border-gray-100">
               <div className="flex items-center justify-center gap-2 text-gray-700 bg-gray-50 p-2 rounded-lg">
                  <Phone size={16} className="text-primary"/>
                  <span className="font-mono text-sm">{userData.phoneNumber || 'Sem telefone'}</span>
               </div>
            </div>
          </div>
        </div>

        <div className="lg:col-span-2">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
             <AddressManager />
          </div>
        </div>
      </div>
    </div>
  );
};