import { useEffect, useState, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { CheckCircle, Package, ArrowRight, AlertTriangle, Loader } from 'lucide-react';
import { Button } from '../../../components/ui/Button';
import { PaymentService } from '../../../services/paymentService';
import confetti from 'canvas-confetti';

export const Success = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const sessionId = searchParams.get('session_id');
  const orderId = searchParams.get('order_id'); 

  const [status, setStatus] = useState('verifying');
  const [attempts, setAttempts] = useState(0);
  const maxAttempts = 5; 

  const processingRef = useRef(false);

  useEffect(() => {
    if (processingRef.current) return;
    
    if (!orderId) {
      setStatus('issue');
      return;
    }

    processingRef.current = true;
    checkPaymentStatus();
  }, [orderId, navigate]);

  const triggerConfetti = () => {
      confetti({ particleCount: 150, spread: 70, origin: { y: 0.6 } });
  };

  const checkPaymentStatus = async () => {
    try {
      const orderData = await PaymentService.getPaymentStatus(orderId);
      
      if (orderData.status === 'Pago' || orderData.status === 'Paid') {
        setStatus('success');
        triggerConfetti();
      } 
      else if (orderData.status === 'Cancelado' || orderData.status === 'Falha') {
        setStatus('issue'); 
      } 
      else {
        handleRetry();
      }
    } catch (error) {
      console.error("Erro na verificação:", error);
      handleRetry();
    }
  };

  const handleRetry = () => {
    setAttempts(prev => {
      const nextAttempt = prev + 1;
      if (nextAttempt < maxAttempts) {
        setTimeout(() => checkPaymentStatus(), 3000);
        return nextAttempt;
      } else {
        setStatus('processing');
        return nextAttempt;
      }
    });
  };

  const renderContent = () => {
    switch (status) {
      case 'verifying':
        return (
          <>
            <Loader className="w-16 h-16 text-blue-600 animate-spin mx-auto mb-6" />
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Confirmando seu Pedido...</h1>
            <p className="text-gray-600 mb-4">Estamos validando a transação com o banco.</p>
          </>
        );

      case 'success':
        return (
          <>
            <div className="w-24 h-24 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <CheckCircle className="text-green-600 w-12 h-12" />
            </div>
            <h1 className="text-3xl font-extrabold text-gray-900 mb-2">Pagamento Confirmado!</h1>
            <p className="text-gray-600 mb-6">
              Recebemos seu pagamento. Seu pedido já está sendo preparado.
            </p>
          </>
        );

      case 'processing':
        return (
          <>
            <div className="w-24 h-24 bg-blue-50 rounded-full flex items-center justify-center mx-auto mb-6">
              <Package className="text-blue-600 w-12 h-12" />
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Pedido Realizado!</h1>
            <p className="text-gray-600 mb-6">
              Seu pedido foi gerado com sucesso (<strong>#{orderId?.slice(0, 8)}</strong>).<br/>
              Ainda estamos aguardando a confirmação do pagamento pelo banco, mas você já pode visualizá-lo em sua conta.
            </p>
          </>
        );

      case 'issue':
      default:
        return (
          <>
            <div className="w-24 h-24 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <AlertTriangle className="text-orange-600 w-12 h-12" />
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Pedido Registrado</h1>
            <p className="text-gray-600 mb-6">
              O pedido foi criado, mas não conseguimos confirmar o pagamento automaticamente aqui.<br/>
              Por favor, acesse seus pedidos para verificar se é necessário tentar pagar novamente.
            </p>
          </>
        );
    }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center bg-gray-50 px-4">
      <div className="bg-white p-10 rounded-2xl shadow-xl text-center max-w-lg w-full border border-gray-200">
        
        {renderContent()}

        <div className="space-y-3 pt-4">
          <Button className="w-full py-3" variant="primary" onClick={() => navigate('/meus-pedidos', { replace: true })}>
            <Package size={18} className="mr-2" /> 
            {status === 'success' ? 'Acompanhar Meus Pedidos' : 'Ver Status do Pedido'}
          </Button>
          
          <Button className="w-full py-3" variant="ghost" onClick={() => navigate('/', { replace: true })}>
            Voltar para a Loja <ArrowRight size={18} className="ml-2" />
          </Button>
        </div>
      </div>
    </div>
  );
};