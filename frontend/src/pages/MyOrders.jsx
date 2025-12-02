import React, { useEffect, useState } from 'react';
import { CartService } from '../services/cartService';
import { Package, Calendar, MapPin, ChevronDown, ChevronUp, CreditCard, Truck, RefreshCcw } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { Button } from '../components/ui/Button';
import toast from 'react-hot-toast';

export const MyOrders = () => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedOrderId, setExpandedOrderId] = useState(null);

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

  const handlePay = async (e, orderId) => {
    e.stopPropagation();
    const loadingToast = toast.loading("Processando pagamento...");
    try {
        await CartService.payOrder(orderId);
        toast.success("Pagamento aprovado!", { id: loadingToast });
        loadOrders();
    } catch (error) {
        toast.error("Erro no pagamento.", { id: loadingToast });
    }
  };

  const handleRefund = async (e, orderId) => {
    e.stopPropagation();
    if(!window.confirm("Deseja realmente solicitar o reembolso deste pedido?")) return;

    const loadingToast = toast.loading("Enviando solicitação...");
    try {
        await CartService.requestRefund(orderId);
        toast.success("Solicitação enviada!", { id: loadingToast });
        loadOrders();
    } catch (error) {
        toast.error(error.response?.data?.message || "Erro ao solicitar reembolso.", { id: loadingToast });
    }
  };

  if (loading) return (
    <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div>
    </div>
  );

  return (
    <div className="max-w-4xl mx-auto px-4 py-12">
      <h1 className="text-3xl font-bold text-gray-900 mb-8 flex items-center gap-3">
        <Package className="text-blue-600" /> Meus Pedidos
      </h1>

      {orders.length === 0 ? (
        <div className="bg-white p-12 rounded-xl shadow-sm border border-gray-100 text-center">
            <Package size={48} className="mx-auto text-gray-300 mb-4" />
            <h3 className="text-xl font-bold text-gray-700">Nenhum pedido encontrado</h3>
            <p className="text-gray-500 mt-2">Você ainda não realizou nenhuma compra conosco.</p>
        </div>
      ) : (
        <div className="space-y-6">
          {orders.map((order) => (
            <motion.div 
              key={order.id}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden transition-all hover:shadow-md"
            >
              {/* Cabeçalho do Pedido */}
              <div 
                onClick={() => toggleExpand(order.id)}
                className="p-6 cursor-pointer flex flex-col md:flex-row md:items-center justify-between gap-4 bg-gray-50/50"
              >
                <div className="space-y-1">
                    <div className="flex items-center gap-3">
                        <span className="font-bold text-lg text-gray-800">Pedido #{order.id.slice(0, 8).toUpperCase()}</span>
                        <StatusBadge status={order.status} />
                    </div>
                    <div className="text-sm text-gray-500 flex items-center gap-2">
                        <Calendar size={14} /> {new Date(order.orderDate).toLocaleDateString('pt-BR')}
                    </div>
                    {/* Código de Rastreio Visível */}
                    {order.trackingCode && (
                        <div className="text-sm text-blue-600 font-medium flex items-center gap-1 mt-1">
                            <Truck size={14}/> Rastreio: <span className="font-mono bg-blue-100 px-1 rounded">{order.trackingCode}</span>
                        </div>
                    )}
                </div>

                <div className="flex items-center justify-between md:justify-end gap-4">
                    <div className="text-right">
                        <div className="text-xs text-gray-500 uppercase font-bold">Total</div>
                        <div className="text-xl font-bold text-green-600">
                            {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(order.totalAmount)}
                        </div>
                    </div>
                    
                    {/* Ações Rápidas */}
                    <div className="flex gap-2">
                        {order.status === 'Pendente' && (
                            <Button size="sm" onClick={(e) => handlePay(e, order.id)}>
                                <CreditCard size={16} /> Pagar
                            </Button>
                        )}
                        {(order.status === 'Pago' || order.status === 'Enviado') && (
                            <Button size="sm" variant="ghost" onClick={(e) => handleRefund(e, order.id)} className="text-red-500 hover:bg-red-50 hover:text-red-600 border border-red-200" title="Solicitar Reembolso">
                                <RefreshCcw size={16} />
                            </Button>
                        )}
                    </div>

                    {expandedOrderId === order.id ? <ChevronUp className="text-gray-400"/> : <ChevronDown className="text-gray-400"/>}
                </div>
              </div>

              {/* Detalhes Expansíveis */}
              <AnimatePresence>
                {expandedOrderId === order.id && (
                    <motion.div 
                        initial={{ height: 0 }} 
                        animate={{ height: "auto" }} 
                        exit={{ height: 0 }} 
                        className="overflow-hidden border-t border-gray-100"
                    >
                        <div className="p-6 bg-white">
                            <div className="mb-6 flex items-start gap-2 text-gray-600 bg-blue-50 p-3 rounded-lg border border-blue-100">
                                <MapPin size={18} className="mt-0.5 text-blue-600" />
                                <div>
                                    <span className="block font-bold text-blue-800 text-sm">Endereço de Entrega</span>
                                    <span className="text-sm">{order.shippingAddress}</span>
                                </div>
                            </div>

                            <table className="w-full text-left text-sm">
                                <thead className="text-gray-500 border-b">
                                    <tr>
                                        <th className="pb-2 font-medium">Produto</th>
                                        <th className="pb-2 font-medium text-center">Qtd</th>
                                        <th className="pb-2 font-medium text-right">Unitário</th>
                                        <th className="pb-2 font-medium text-right">Total</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y">
                                    {order.items.map((item, idx) => (
                                        <tr key={idx}>
                                            <td className="py-3 font-medium text-gray-800">{item.productName}</td>
                                            <td className="py-3 text-center text-gray-600">{item.quantity}</td>
                                            <td className="py-3 text-right text-gray-600">
                                                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.unitPrice)}
                                            </td>
                                            <td className="py-3 text-right font-bold text-gray-800">
                                                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.total)}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </motion.div>
                )}
              </AnimatePresence>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  );
};

const StatusBadge = ({ status }) => {
    const styles = {
        'Pendente': 'bg-yellow-100 text-yellow-800 border-yellow-200',
        'Pago': 'bg-green-100 text-green-800 border-green-200',
        'Enviado': 'bg-blue-100 text-blue-800 border-blue-200',
        'Entregue': 'bg-gray-100 text-gray-800 border-gray-200',
        'Cancelado': 'bg-red-100 text-red-800 border-red-200',
        'Reembolso Solicitado': 'bg-purple-100 text-purple-800 border-purple-200',
        'Reembolsado': 'bg-gray-800 text-white border-gray-800'
    };

    return (
        <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold border ${styles[status] || styles['Pendente']}`}>
            {status}
        </span>
    );
};