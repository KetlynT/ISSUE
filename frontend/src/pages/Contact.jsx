import React, { useEffect, useState } from 'react';
import { Phone, MapPin, Send, Mail } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { ContentService } from '../services/contentService';

export const Contact = () => {
  const [settings, setSettings] = useState({
    whatsapp_display: 'Carregando...',
    contact_email: 'Carregando...',
    address: 'Carregando...'
  });

  useEffect(() => {
    const loadSettings = async () => {
      const data = await ContentService.getSettings();
      if(data) setSettings(prev => ({...prev, ...data}));
    };
    loadSettings();
  }, []);

  const handleSubmit = (e) => {
    e.preventDefault();
    // Simulação de envio
    alert("Mensagem enviada com sucesso!");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-16">
      <h1 className="text-4xl font-bold text-center mb-12 text-gray-800">Fale Conosco</h1>
      
      <div className="grid md:grid-cols-2 gap-12">
        {/* Informações de Contato Dinâmicas */}
        <div className="space-y-8">
          <div className="bg-blue-50 p-6 rounded-2xl border border-blue-100">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-blue-800">
              <Phone className="text-blue-600" /> WhatsApp
            </h3>
            <p className="text-gray-600">Atendimento rápido para orçamentos e dúvidas.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.whatsapp_display}</p>
          </div>

          <div className="bg-blue-50 p-6 rounded-2xl border border-blue-100">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-blue-800">
              <Mail className="text-blue-600" /> E-mail
            </h3>
            <p className="text-gray-600">Envie seus arquivos ou solicitações formais.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.contact_email}</p>
          </div>

          <div className="bg-blue-50 p-6 rounded-2xl border border-blue-100">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-blue-800">
              <MapPin className="text-blue-600" /> Endereço
            </h3>
            <p className="text-gray-600">Venha tomar um café conosco em nossa sede.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.address}</p>
          </div>
        </div>

        {/* Formulário */}
        <form onSubmit={handleSubmit} className="bg-white p-8 rounded-2xl shadow-lg border border-gray-100">
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nome Completo</label>
              <input 
                type="text" 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-blue-500 transition-all" 
                placeholder="Seu nome"
                required 
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
              <input 
                type="email" 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-blue-500 transition-all" 
                placeholder="seu@email.com"
                required 
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Como podemos ajudar?</label>
              <textarea 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-blue-500 transition-all h-32 resize-none" 
                placeholder="Descreva seu projeto ou dúvida..."
                required
              ></textarea>
            </div>
            <Button type="submit" className="w-full">
              <Send size={18} /> Enviar Mensagem
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};