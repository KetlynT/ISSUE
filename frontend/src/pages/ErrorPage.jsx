import { useEffect } from 'react';
import { AlertTriangle, RefreshCcw } from 'lucide-react';
import { Button } from '../components/ui/Button';

export const ErrorPage = () => {
  
  useEffect(() => {
    const navEntry = performance.getEntriesByType("navigation")[0];
    if (navEntry && navEntry.type === 'reload') {
       handleRetry();
    }
  }, []);

  const handleRetry = () => {
    const lastRoute = sessionStorage.getItem('last_valid_route') || '/';
    const savedConsent = localStorage.getItem('lgpd_consent');
    
    localStorage.clear();
    sessionStorage.clear();
    
    if (savedConsent) {
        localStorage.setItem('lgpd_consent', savedConsent);
    }

    window.location.href = lastRoute;
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-xl border border-gray-100 p-8 text-center">
        <div className="w-20 h-20 bg-red-50 rounded-full flex items-center justify-center mx-auto mb-6">
          <AlertTriangle className="text-red-500 w-10 h-10" />
        </div>
        
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Ops! Algo deu errado.</h1>
        <p className="text-gray-500 mb-8 leading-relaxed">
          Não foi possível conectar ao sistema. Estamos passando por uma breve instabilidade ou manutenção.
        </p>

        <div className="space-y-4">
          <Button 
            onClick={handleRetry} 
            className="w-full py-3 shadow-red-100"
          >
            <RefreshCcw size={18} /> Tentar Novamente
          </Button>
          
          <p className="text-xs text-gray-400 mt-4">
            Se o problema persistir, aguarde alguns instantes.
          </p>
        </div>
      </div>
    </div>
  );
};