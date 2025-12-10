import { useEffect, useState } from 'react';
import { Phone, MapPin, Send, Mail } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { ContentService } from '../services/contentService';
import toast from 'react-hot-toast';

export const Contact = () => {
  const [settings, setSettings] = useState({
    whatsapp_display: 'Carregando...',
    contact_email: 'Carregando...',
    address: 'Carregando...'
  });

  const [honey, setHoney] = useState('');

  useEffect(() => {
    const loadSettings = async () => {
      const data = await ContentService.getSettings();
      if(data) setSettings(prev => ({...prev, ...data}));
    };
    loadSettings();
  }, []);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (honey) {
        console.log("Bot detectado e bloqueado.");
        toast.success("Mensagem enviada com sucesso!");
        return;
    }
    toast.success("Mensagem enviada com sucesso!");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-16">
      <h1 className="text-4xl font-bold text-center mb-12 text-gray-800">Fale Conosco</h1>
      
      <div className="grid md:grid-cols-2 gap-12">
        <div className="space-y-8">
          <div className="bg-primary/5 p-6 rounded-2xl border border-primary/10">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-secondary">
              <Phone className="text-primary" /> WhatsApp
            </h3>
            <p className="text-gray-600">Atendimento rápido para orçamentos e dúvidas.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.whatsapp_display}</p>
          </div>

          <div className="bg-primary/5 p-6 rounded-2xl border border-primary/10">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-secondary">
              <Mail className="text-primary" /> E-mail
            </h3>
            <p className="text-gray-600">Envie seus arquivos ou solicitações formais.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.contact_email}</p>
          </div>

          <div className="bg-primary/5 p-6 rounded-2xl border border-primary/10">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2 text-secondary">
              <MapPin className="text-primary" /> Endereço
            </h3>
            <p className="text-gray-600">Venha tomar um café conosco em nossa sede.</p>
            <p className="font-bold text-lg mt-2 text-gray-800">{settings.address}</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-white p-8 rounded-2xl shadow-lg border border-gray-100">
          <div className="space-y-4">
            <input 
                type="text" 
                name="website_url" 
                style={{ display: 'none', opacity: 0, position: 'absolute', left: '-9999px' }} 
                tabIndex="-1" 
                autoComplete="off"
                value={honey}
                onChange={(e) => setHoney(e.target.value)}
            />

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nome Completo</label>
              <input 
                type="text" 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-primary transition-all" 
                placeholder="Seu nome"
                required 
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
              <input 
                type="email" 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-primary transition-all" 
                placeholder="seu@email.com"
                required 
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Como podemos ajudar?</label>
              <textarea 
                className="w-full border border-gray-300 rounded-lg p-3 outline-none focus:ring-2 focus:ring-primary transition-all h-32 resize-none" 
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