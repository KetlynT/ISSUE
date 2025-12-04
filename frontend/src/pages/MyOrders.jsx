import React, { useEffect, useState } from 'react';
import { CartService } from '../services/cartService';
import { PaymentService } from '../services/paymentService'; // Importar PaymentService
import { Package, Calendar, MapPin, ChevronDown, ChevronUp, CreditCard, Truck, RefreshCcw, AlertTriangle, Clock, Box, Info } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { Button } from '../components/ui/Button';
import toast from 'react-hot-toast';

export const MyOrders = () => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedOrderId, setExpandedOrderId] = useState(null);

  const [isRefundModalOpen, setIsRefundModalOpen] = useState(false);
  const [selectedOrderForRefund, setSelectedOrderForRefund] = useState(null);

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    try {
      const data = await CartService.getMyOrders();
      setOrders(data);
    } catch (error) {
      console.error("Erro ao carregar pedidos", error);
      toast.error("Não foi possível carregar seus pedidos.");
    } finally {
      setLoading(false);
    }
  };

  const toggleExpand = (id) => {
    setExpandedOrderId(expandedOrderId === id ? null : id);
  };

  // NOVA LÓGICA: Pagar via Stripe para pedidos pendentes
  const handlePay = async (e, orderId) => {
    e.stopPropagation();
    const loadingToast = toast.loading("Iniciando pagamento...");
    try {
        const { url } = await PaymentService.createCheckoutSession(orderId);
        window.location.href = url; // Redireciona para Stripe
    } catch (error) {
        toast.error("Erro ao iniciar pagamento.", { id: loadingToast });
    }
  };

  const openRefundModal = (orderId) => {
    setSelectedOrderForRefund(orderId);
    setIsRefundModalOpen(true);
  };

  const confirmRefund = async () => {
    if (!selectedOrderForRefund) return;
    const loadingToast = toast.loading("Enviando solicitação...");
    try {
        await CartService.requestRefund(selectedOrderForRefund);
        toast.success("Solicitação enviada!", { id: loadingToast });
        loadOrders();
    } catch (error) {
        toast.error(error.response?.data?.message || "Erro ao solicitar reembolso.", { id: loadingToast });
    } finally {
        setIsRefundModalOpen(false);
        setSelectedOrderForRefund(null);
    }
  };

  const getRefundStatus = (order) => {
    const status = order.status;
    const validStatuses = ['Pago', 'Enviado', 'Entregue'];
    
    if (!validStatuses.includes(status)) {
        return { showSection: false, canRefund: false, label: '' };
    }

    if (status === 'Pago' || status === 'Enviado') {
        return { showSection: true, canRefund: true, label: "Solicitar Cancelamento" };
    }

    if (status === 'Entregue') {
        if (!order.deliveryDate) return { showSection: true, canRefund: false, label: "Aguardando data..." };
        const deadline = new Date(new Date(order.deliveryDate).setDate(new Date(order.deliveryDate).getDate() + 7));
        const canRefund = new Date() <= deadline;
        return { showSection: true, canRefund, label: canRefund ? "Solicitar Devolução" : "Prazo Expirado" };
    }
    return { showSection: false, canRefund: false, label: '' };
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center"><div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div></div>;

  return (
    <div className="max-w-4xl mx-auto px-4 py-12">
      <h1 className="text-3xl font-bold text-gray-900 mb-8 flex items-center gap-3"><Package className="text-blue-600" /> Meus Pedidos</h1>

      {orders.length === 0 ? (
        <div className="bg-white p-12 rounded-xl shadow-sm border border-gray-100 text-center">
            <Package size={48} className="mx-auto text-gray-300 mb-4" />
            <h3 className="text-xl font-bold text-gray-700">Nenhum pedido encontrado</h3>
        </div>
      ) : (
        <div className="space-y-6">
          {orders.map((order) => {
            const { showSection, canRefund, label } = getRefundStatus(order);

            return (
            <motion.div key={order.id} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden transition-all hover:shadow-md">
              <div onClick={() => toggleExpand(order.id)} className="p-6 cursor-pointer flex flex-col md:flex-row md:items-center justify-between gap-4 bg-gray-50/50">
                <div className="space-y-1">
                    <div className="flex items-center gap-3">
                        <span className="font-bold text-lg text-gray-800">Pedido #{order.id.slice(0, 8).toUpperCase()}</span>
                        <StatusBadge status={order.status} />
                    </div>
                    <div className="text-sm text-gray-500 flex items-center gap-2"><Calendar size={14} /> {new Date(order.orderDate).toLocaleDateString('pt-BR')}</div>
                </div>
                <div className="flex items-center justify-between md:justify-end gap-4">
                    <div className="text-right">
                        <div className="text-xs text-gray-500 uppercase font-bold">Total</div>
                        <div className="text-xl font-bold text-green-600">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.totalAmount)}</div>
                    </div>
                    {/* Botão de Pagar agora leva para o Stripe */}
                    {order.status === 'Pendente' && <Button size="sm" onClick={(e) => handlePay(e, order.id)}><CreditCard size={16} /> Pagar</Button>}
                    {expandedOrderId === order.id ? <ChevronUp className="text-gray-400"/> : <ChevronDown className="text-gray-400"/>}
                </div>
              </div>

              <AnimatePresence>
                {expandedOrderId === order.id && (
                    <motion.div initial={{ height: 0 }} animate={{ height: "auto" }} exit={{ height: 0 }} className="overflow-hidden border-t border-gray-100">
                        <div className="p-6 bg-white">
                            {order.reverseLogisticsCode && (
                                <div className="mb-6 bg-orange-50 border border-orange-200 rounded-lg p-4">
                                    <h4 className="text-orange-800 font-bold flex items-center gap-2 mb-2"><Box size={18}/> Instruções de Devolução</h4>
                                    <p className="text-sm text-orange-900 mb-3">{order.returnInstructions || "Leve o produto até uma agência dos Correios."}</p>
                                    <div className="bg-white border border-orange-200 p-3 rounded flex items-center justify-between">
                                        <span className="text-xs text-gray-500 uppercase font-bold">Código de Postagem</span>
                                        <span className="font-mono text-lg font-bold text-gray-800 tracking-wider">{order.reverseLogisticsCode}</span>
                                    </div>
                                </div>
                            )}

                            <div className="mb-6 flex flex-col md:flex-row justify-between gap-4">
                                <div className="flex items-start gap-2 text-gray-600 bg-blue-50 p-3 rounded-lg border border-blue-100 flex-1">
                                    <MapPin size={18} className="mt-0.5 text-blue-600" />
                                    <div><span className="block font-bold text-blue-800 text-sm">Endereço</span><span className="text-sm">{order.shippingAddress}</span></div>
                                </div>
                                <div className="flex-1 space-y-2">
                                    {order.trackingCode && <div className="text-sm text-gray-700 bg-gray-50 p-2 rounded border border-gray-200 flex items-center gap-2"><Truck size={16} className="text-blue-600"/> Rastreio: <span className="font-mono font-bold">{order.trackingCode}</span></div>}
                                    {order.deliveryDate && <div className="text-xs text-gray-500 flex items-center gap-2"><Clock size={14}/> Entregue: {new Date(order.deliveryDate).toLocaleDateString('pt-BR')}</div>}
                                </div>
                            </div>

                            <table className="w-full text-left text-sm mb-6">
                                <tbody className="divide-y">
                                    {order.items.map((item, idx) => (
                                        <tr key={idx}>
                                            <td className="py-2 font-medium text-gray-800">{item.quantity}x {item.productName}</td>
                                            <td className="py-2 text-right font-bold text-gray-800">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.total)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>

                            {showSection && (
                                <div className="border-t pt-3 flex justify-end">
                                    <button
                                        disabled={!canRefund}
                                        onClick={() => openRefundModal(order.id)}
                                        className={`text-xs font-medium px-3 py-1.5 rounded transition-colors flex items-center gap-1.5
                                            ${canRefund 
                                                ? 'text-red-600 hover:bg-red-50 border border-red-200' 
                                                : 'text-gray-400 bg-gray-50 border border-gray-200 cursor-not-allowed opacity-70'}`}
                                    >
                                        <RefreshCcw size={12} /> {label}
                                    </button>
                                </div>
                            )}
                        </div>
                    </motion.div>
                )}
              </AnimatePresence>
            </motion.div>
          )})}
        </div>
      )}

      {/* Modal de Confirmação */}
      {isRefundModalOpen && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">
                <div className="p-6 text-center">
                    <div className="mx-auto bg-red-100 w-12 h-12 rounded-full flex items-center justify-center mb-4"><AlertTriangle className="text-red-600" size={24} /></div>
                    <h3 className="text-lg font-bold text-gray-900 mb-2">Iniciar Processo?</h3>
                    <p className="text-gray-500 text-sm mb-6">Sua solicitação será analisada pela equipe. Se aprovada, você receberá instruções.</p>
                    <div className="flex justify-center gap-3">
                        <Button variant="ghost" onClick={() => setIsRefundModalOpen(false)}>Voltar</Button>
                        <Button variant="danger" onClick={confirmRefund}>Confirmar</Button>
                    </div>
                </div>
            </div>
        </div>
      )}
    </div>
  );
};

const StatusBadge = ({ status }) => {
    const styles = {
        'Pendente': 'bg-yellow-100 text-yellow-800', 'Pago': 'bg-green-100 text-green-800', 'Enviado': 'bg-blue-100 text-blue-800',
        'Entregue': 'bg-gray-100 text-gray-800', 'Cancelado': 'bg-red-100 text-red-800', 
        'Reembolso Solicitado': 'bg-purple-100 text-purple-800', 'Aguardando Devolução': 'bg-orange-100 text-orange-800',
        'Reembolsado': 'bg-gray-800 text-white'
    };
    return <span className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide ${styles[status] || 'bg-gray-100'}`}>{status}</span>;
};