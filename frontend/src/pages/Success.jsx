import { useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { CheckCircle, Package, ArrowRight } from 'lucide-react';
import { Button } from '../components/ui/Button';
import confetti from 'canvas-confetti'; // Instalar se quiser efeito: npm install canvas-confetti

export const Success = () => {
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session_id');

  useEffect(() => {
    // Dispara confetes se houver session_id (indica retorno do Stripe)
    if (sessionId) {
      confetti({
        particleCount: 100,
        spread: 70,
        origin: { y: 0.6 }
      });
    }
  }, [sessionId]);

  return (
    <div className="min-h-[80vh] flex items-center justify-center bg-gray-50 px-4">
      <div className="bg-white p-10 rounded-2xl shadow-xl text-center max-w-lg w-full border border-green-100">
        <div className="w-24 h-24 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
          <CheckCircle className="text-green-600 w-12 h-12" />
        </div>
        
        <h1 className="text-3xl font-extrabold text-gray-900 mb-2">Pagamento Confirmado!</h1>
        <p className="text-gray-600 mb-8">
          Obrigado pela sua compra. Recebemos seu pagamento e já estamos preparando seus arquivos para impressão.
        </p>

        <div className="bg-gray-50 p-4 rounded-lg mb-8 text-sm text-gray-500">
            ID da Transação: <span className="font-mono text-gray-700 block mt-1 break-all">{sessionId || 'N/A'}</span>
        </div>

        <div className="space-y-3">
          <Link to="/meus-pedidos">
            <Button className="w-full py-3" variant="primary">
              <Package size={18} /> Acompanhar Meus Pedidos
            </Button>
          </Link>
          <Link to="/">
            <Button className="w-full py-3" variant="ghost">
              Voltar para a Loja <ArrowRight size={18} />
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
};