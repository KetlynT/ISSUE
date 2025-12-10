import { useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { Button } from './ui/Button';
import { ShieldCheck } from 'lucide-react';

export const CookieConsent = () => {
  const [isVisible, setIsVisible] = useState(false);
  const location = useLocation();

  useEffect(() => {
    const showBanner = () => {
      const consent = localStorage.getItem('lgpd_consent');
      if (!consent) {
        setIsVisible(true);
      }
    };

    const delayedShow = () => {
      setTimeout(showBanner, 3000);
    };

    if (document.readyState === 'complete') {
      delayedShow();
    } else {
      window.addEventListener('load', showBanner);
      return () => window.removeEventListener('load', showBanner);
    }
  }, []);

  const handleAccept = () => {
    const consentData = {
      accepted: true,
      timestamp: new Date().toISOString(),
      ipParams: 'captured_on_server'
    };
    localStorage.setItem('lgpd_consent', JSON.stringify(consentData));
    setIsVisible(false);
  };

  const isBlockedRoute = ['/error', '/login', '/putiroski', '/cadastro', '/esqueci-senha', '/reset-password', '/confirm-email'].some(path => 
    location.pathname.startsWith(path)
  );

  if (!isVisible || isBlockedRoute) return null;

  return (
    <div className="fixed bottom-0 left-0 w-full bg-gray-900/95 backdrop-blur text-white p-6 z-[100] border-t border-gray-700 shadow-2xl animate-in slide-in-from-bottom duration-500">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-6">
        <div className="flex-1">
          <h4 className="text-lg font-bold flex items-center gap-2 mb-2 text-blue-400">
            <ShieldCheck size={24} /> Privacidade e Transparência
          </h4>
          <p className="text-sm text-gray-300 leading-relaxed">
            Utilizamos cookies essenciais para manter sua sessão segura e funcional. 
            Para fins de segurança e cumprimento legal (Marco Civil da Internet), 
            registramos seu endereço IP durante transações e acessos administrativos. 
            Ao continuar, você concorda com nossa <a href="/pagina/politica" className="underline hover:text-blue-400">Política de Privacidade</a>.
          </p>
        </div>
        <div className="flex gap-4 min-w-fit">
          <Button 
            variant="ghost" 
            className="text-gray-400 hover:text-white hover:bg-white/10"
            onClick={() => setIsVisible(false)}
          >
            Agora não
          </Button>
          <Button 
            variant="primary" 
            className="bg-blue-600 hover:bg-blue-500 text-white shadow-blue-900/50"
            onClick={handleAccept}
          >
            Entendi e Concordo
          </Button>
        </div>
      </div>
    </div>
  );
};