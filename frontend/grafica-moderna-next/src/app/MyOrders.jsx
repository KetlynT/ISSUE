import { useEffect, useState } from 'react';
import { OrderService } from '../services/orderService';
import { PaymentService } from '../services/paymentService'; 
import { Package, Calendar, MapPin, ChevronDown, ChevronUp, CreditCard, Truck, RefreshCcw, AlertTriangle, Clock, Box, ChevronLeft, ChevronRight } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { Button } from '../components/ui/Button';
import toast from 'react-hot-toast';
import RefundRequestModal from '../components/RefundRequestModal';

export const MyOrders = () => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedOrderId, setExpandedOrderId] = useState(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  const [selectedOrderForRefund, setSelectedOrderForRefund] = useState(null);
  const [isRefundLoading, setIsRefundLoading] = useState(false);

  useEffect(() => { loadOrders(); }, [page]);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const data = await OrderService.getMyOrders(page);
      setOrders(data.items || []);
      setTotalPages(data.totalPages || 1);
    } catch (error) {
      toast.error("Não foi possível carregar seus pedidos.");
    } finally {
      setLoading(false);
    }
  };

  const toggleExpand = (id) => setExpandedOrderId(expandedOrderId === id ? null : id);

  const handlePay = async (e, orderId) => {
    e.stopPropagation();
    const loadingToast = toast.loading("Iniciando pagamento...");
    try {
        const { url } = await PaymentService.createCheckoutSession(orderId);
        window.location.href = url;
    } catch (error) {
        toast.error("Erro ao iniciar pagamento.", { id: loadingToast });
    }
  };

  const handleOpenRefundModal = (order) => {
    setSelectedOrderForRefund(order);
  };

  const handleRefundSubmit = async (payload) => {
    if (!selectedOrderForRefund) return;
    
    const loadingToast = toast.loading("Enviando solicitação...");
    setIsRefundLoading(true);

    try {
        await OrderService.requestRefund(selectedOrderForRefund.id, payload);
        toast.success("Solicitação enviada com sucesso!", { id: loadingToast });
        await loadOrders();
        setSelectedOrderForRefund(null);
    } catch (error) {
        toast.error(error.response?.data?.message || "Erro ao solicitar reembolso.", { id: loadingToast });
    } finally {
        setIsRefundLoading(false);
    }
  };

  const getRefundStatus = (order) => {
    const status = order.status;
    const validStatuses = ['Pago', 'Enviado', 'Entregue'];
    
    if (!validStatuses.includes(status)) return { showSection: false, canRefund: false, label: '' };
    
    if (status === 'Pago' || status === 'Enviado') return { showSection: true, canRefund: true, label: "Solicitar Cancelamento" };
    
    if (status === 'Entregue') {
        if (!order.deliveryDate) return { showSection: true, canRefund: false, label: "Aguardando data..." };
        const deadline = new Date(new Date(order.deliveryDate).setDate(new Date(order.deliveryDate).getDate() + 7));
        const canRefund = new Date() <= deadline;
        return { showSection: true, canRefund, label: canRefund ? "Solicitar Devolução" : "Prazo Expirado" };
    }
    return { showSection: false, canRefund: false, label: '' };
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center"><div className="animate-spin rounded-full h-12 w-12 border-4 border-primary border-t-transparent"></div></div>;

  return (
    <div className="max-w-4xl mx-auto px-4 py-12">
      <h1 className="text-3xl font-bold text-gray-900 mb-8 flex items-center gap-3"><Package className="text-primary" /> Meus Pedidos</h1>

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
                    {order.status === 'Pendente' && <Button size="sm" onClick={(e) => handlePay(e, order.id)}><CreditCard size={16} /> Pagar</Button>}
                    {expandedOrderId === order.id ? <ChevronUp className="text-gray-400"/> : <ChevronDown className="text-gray-400"/>}
                </div>
              </div>

              <AnimatePresence>
                {order.status === 'Reembolso Reprovado' && (
                    <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 mx-6 mt-4">
                        <h4 className="text-red-800 font-bold flex items-center gap-2 mb-2">
                            <AlertTriangle size={18}/> Solicitação de Reembolso Negada
                        </h4>
                        
                        <div className="space-y-3">
                            <div>
                                <span className="text-xs font-bold text-red-700 uppercase block mb-1">Motivo da Análise:</span>
                                <p className="text-sm text-gray-800 bg-white p-3 rounded border border-red-100">
                                    {order.refundRejectionReason || "Entre em contato com o suporte para mais detalhes."}
                                </p>
                            </div>

                            {order.refundRejectionProof && (
                                <div>
                                    <span className="text-xs font-bold text-red-700 uppercase block mb-1">Evidência Anexada:</span>
                                    <div className="mt-2">
                                        {order.refundRejectionProof.endsWith('.mp4') || order.refundRejectionProof.endsWith('.webm') ? (
                                            <video controls className="w-full max-w-sm rounded border border-gray-300 max-h-64">
                                                <source src={order.refundRejectionProof} type="video/mp4" />
                                                Seu navegador não suporta vídeos.
                                            </video>
                                        ) : (
                                            <a href={order.refundRejectionProof} target="_blank" rel="noopener noreferrer">
                                                <img 
                                                    src={order.refundRejectionProof} 
                                                    alt="Prova" 
                                                    className="h-32 w-auto object-cover rounded border border-gray-300 hover:opacity-90 transition-opacity" 
                                                />
                                            </a>
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}
                
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
                                <div className="flex items-start gap-2 text-gray-600 bg-primary/5 p-3 rounded-lg border border-primary/10 flex-1">
                                    <MapPin size={18} className="mt-0.5 text-primary" />
                                    <div><span className="block font-bold text-primary text-sm">Endereço</span><span className="text-sm">{order.shippingAddress}</span></div>
                                </div>
                                <div className="flex-1 space-y-2">
                                    {order.trackingCode && <div className="text-sm text-gray-700 bg-gray-50 p-2 rounded border border-gray-200 flex items-center gap-2"><Truck size={16} className="text-primary"/> Rastreio: <span className="font-mono font-bold">{order.trackingCode}</span></div>}
                                    {order.deliveryDate && <div className="text-xs text-gray-500 flex items-center gap-2"><Clock size={14}/> Entregue: {new Date(order.deliveryDate).toLocaleDateString('pt-BR')}</div>}
                                </div>
                            </div>

                            <table className="w-full text-left text-sm mb-4">
                                <tbody className="divide-y border-b border-gray-100">
                                    {order.items.map((item, idx) => (
                                        <tr key={idx}>
                                            <td className="py-2 font-medium text-gray-800">{item.quantity}x {item.productName}</td>
                                            <td className="py-2 text-right font-bold text-gray-800">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.total)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>

                            <div className="flex flex-col items-end gap-1 text-sm text-gray-700 mb-6">
                                <div className="flex justify-between w-full max-w-[240px]"><span>Subtotal:</span><span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.subTotal || 0)}</span></div>
                                {order.discount > 0 && (
                                    <div className="flex justify-between w-full max-w-[240px] text-green-600"><span>Desconto:</span><span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.discount)}</span></div>
                                )}
                                <div className="flex justify-between w-full max-w-[240px] text-primary"><span>Frete:</span><span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.shippingCost || 0)}</span></div>
                                <div className="flex justify-between w-full max-w-[240px] font-bold text-lg mt-2 border-t pt-2 border-gray-200"><span>Total:</span><span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.totalAmount)}</span></div>
                            </div>

                            {showSection && (
                                <div className="border-t pt-3 flex justify-end">
                                    <button
                                        disabled={!canRefund}
                                        onClick={() => handleOpenRefundModal(order)}
                                        className={`text-xs font-medium px-3 py-1.5 rounded transition-colors flex items-center gap-1.5
                                            ${canRefund ? 'text-red-600 hover:bg-red-50 border border-red-200' : 'text-gray-400 bg-gray-50 border border-gray-200 cursor-not-allowed opacity-70'}`}
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
          
            {totalPages > 1 && (
              <div className="flex justify-center items-center gap-4 mt-8 pt-6 border-t border-gray-100">
                <button
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="p-2 rounded-lg border bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  <ChevronLeft size={20} />
                </button>
                <span className="text-sm font-medium text-gray-600">
                  Página {page} de {totalPages}
                </span>
                <button
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="p-2 rounded-lg border bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  <ChevronRight size={20} />
                </button>
              </div>
            )}
        </div>
      )}
      
      {selectedOrderForRefund && (
        <RefundRequestModal
            order={selectedOrderForRefund}
            onClose={() => setSelectedOrderForRefund(null)}
            onSubmit={handleRefundSubmit}
            isLoading={isRefundLoading}
        />
      )}
    </div>
  );
};

const StatusBadge = ({ status }) => {
    const styles = {
        'Pendente': 'bg-yellow-100 text-yellow-800', 'Pago': 'bg-green-100 text-green-800', 'Enviado': 'bg-blue-100 text-blue-800',
        'Entregue': 'bg-gray-100 text-gray-800', 'Cancelado': 'bg-red-100 text-red-800', 
        'Reembolso Solicitado': 'bg-purple-100 text-purple-800', 'Aguardando Devolução': 'bg-orange-100 text-orange-800',
        'Reembolsado': 'bg-gray-800 text-white', 'Reembolso Reprovado': 'bg-red-200 text-red-900', 'Parcialmente Reembolsado': 'bg-purple-200 text-purple-900'
    };
    return <span className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide ${styles[status] || 'bg-gray-100'}`}>{status}</span>;
};