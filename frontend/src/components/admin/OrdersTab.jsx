import { useEffect, useState } from 'react';
import { OrderService } from '../../services/orderService';
import { ProductService } from '../../services/productService';
import { Button } from '../../components/ui/Button';
import toast from 'react-hot-toast';
import { Search, Eye, X, Settings, RefreshCcw } from 'lucide-react';

const OrdersTab = () => {
    const [orders, setOrders] = useState([]);
    const [filteredOrders, setFilteredOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [statusFilter, setStatusFilter] = useState('Todos');
    const [refundReason, setRefundReason] = useState('');
    const [refundProof, setRefundProof] = useState('');
    const [uploadingProof, setUploadingProof] = useState(false);

    const [selectedOrder, setSelectedOrder] = useState(null);
    const [trackingInput, setTrackingInput] = useState('');
    const [statusInput, setStatusInput] = useState('');
    const [reverseCodeInput, setReverseCodeInput] = useState('');
    const [returnInstructionsInput, setReturnInstructionsInput] = useState('');

    useEffect(() => { loadOrders(); }, []);

    useEffect(() => {
        let result = orders;
        if(statusFilter !== 'Todos') {
            result = result.filter(o => o.status === statusFilter);
        }
        if(searchTerm) {
            const lowerTerm = searchTerm.toLowerCase();
            result = result.filter(o => 
                o.id.toLowerCase().includes(lowerTerm) || 
                (o.customerName && o.customerName.toLowerCase().includes(lowerTerm)) ||
                (o.customerCpf && o.customerCpf.includes(lowerTerm)) ||
                (o.customerEmail && o.customerEmail.toLowerCase().includes(lowerTerm))
            );
        }
        setFilteredOrders(result);
    }, [orders, statusFilter, searchTerm]);

    const loadOrders = async () => {
        try {
            const data = await OrderService.getAllOrders();
            setOrders(data);
        } catch (e) {
            toast.error("Erro ao carregar pedidos.");
        } finally {
            setLoading(false);
        }
    };

    const handleOpenModal = (order) => {
        setSelectedOrder(order);
        setStatusInput(order.status);
        setTrackingInput(order.trackingCode || '');
        setReverseCodeInput(order.reverseLogisticsCode || '');
        setReturnInstructionsInput(order.returnInstructions || '');
        setRefundReason(order.refundRejectionReason || '');
        setRefundProof(order.refundRejectionProof || '');
    };

    const handleCloseModal = () => {
        setSelectedOrder(null);
    };

    const handleProofUpload = async (e) => {
        const file = e.target.files[0];
        if (!file) return;
        setUploadingProof(true);
        try {
            const url = await ProductService.uploadImage(file); 
            setRefundProof(url);
            toast.success("Arquivo anexado!");
        } catch (error) {
            toast.error("Erro no upload.");
        } finally {
            setUploadingProof(false);
        }
    };

    const handleUpdateOrder = async (e) => {
        e.preventDefault();
        const toastId = toast.loading("Atualizando...");
        try {
            await OrderService.updateOrderStatus(selectedOrder.id, {
                status: statusInput,
                trackingCode: trackingInput,
                reverseLogisticsCode: reverseCodeInput,
                returnInstructions: returnInstructionsInput,
                refundRejectionReason: refundReason,
                refundRejectionProof: refundProof
            });
            
            const updatedList = orders.map(o => o.id === selectedOrder.id ? {
                ...o, 
                status: statusInput, 
                trackingCode: trackingInput,
                reverseLogisticsCode: reverseCodeInput,
                returnInstructions: returnInstructionsInput,
                refundRejectionReason: refundReason,
                refundRejectionProof: refundProof
            } : o);
            
            setOrders(updatedList);
            toast.success("Pedido atualizado com sucesso!", { id: toastId });
            handleCloseModal();
        } catch (err) {
            toast.error("Erro ao atualizar pedido.", { id: toastId });
        }
    };

    if (loading) return <div className="text-center py-10">Carregando pedidos...</div>;

    return (
        <div>
            <div className="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
                <h2 className="text-xl font-bold text-gray-800">Gerenciamento de Pedidos</h2>
                <div className="flex gap-2 w-full md:w-auto">
                    <div className="relative flex-grow">
                        <Search className="absolute left-3 top-2.5 text-gray-400" size={18} />
                        <input 
                            className="pl-10 pr-4 py-2 border rounded-lg w-full md:w-64 focus:ring-2 focus:ring-blue-500 outline-none"
                            placeholder="Buscar ID, Nome, CPF/CNPJ ou Email..."
                            value={searchTerm}
                            onChange={e => setSearchTerm(e.target.value)}
                        />
                    </div>
                    <select 
                        className="border rounded-lg px-3 py-2 bg-white outline-none focus:ring-2 focus:ring-blue-500"
                        value={statusFilter}
                        onChange={e => setStatusFilter(e.target.value)}
                    >
                        <option value="Todos">Todos os Status</option>
                        <option value="Pendente">Pendente</option>
                        <option value="Pago">Pago</option>
                        <option value="Enviado">Enviado</option>
                        <option value="Entregue">Entregue</option>
                        <option value="Cancelado">Cancelado</option>
                        <option value="Reembolso Solicitado">Reembolso Solicitado</option>
                        <option value="Aguardando Devolução">Aguardando Devolução</option>
                        <option value="Reembolsado">Reembolsado</option>
                        <option value="Reembolso Reprovado">Reembolso Reprovado</option>
                    </select>
                </div>
            </div>

            <div className="bg-white rounded-lg shadow border overflow-hidden">
                <table className="w-full text-left text-sm">
                    <thead className="bg-gray-50 border-b text-gray-600 uppercase">
                        <tr>
                            <th className="p-4">Pedido</th>
                            <th className="p-4">Cliente</th>
                            <th className="p-4">Status</th>
                            <th className="p-4 text-right">Total</th>
                            <th className="p-4 text-right">Ações</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {filteredOrders.map(o => (
                            <tr key={o.id} className="hover:bg-gray-50">
                                <td className="p-4 align-middle">
                                    <div className="font-bold text-gray-800 font-mono">#{o.id.slice(0, 8)}</div>
                                    <div className="text-xs text-gray-500">{new Date(o.orderDate).toLocaleDateString('pt-BR')}</div>
                                </td>
                                <td className="p-4 align-middle">
                                    <div className="font-bold text-gray-800">{o.customerName || 'Cliente'}</div>
                                    <div className="text-xs text-gray-500 flex flex-col">
                                        <span>{o.customerCpf}</span>
                                        <span>{o.customerEmail}</span>
                                    </div>
                                </td>
                                <td className="p-4 align-middle">
                                    <OrderStatusBadge status={o.status} />
                                    {o.trackingCode && (
                                        <div className="text-xs text-gray-500 mt-1 font-mono">Rastreio: {o.trackingCode}</div>
                                    )}
                                </td>
                                <td className="p-4 text-right align-middle font-bold text-green-700">
                                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(o.totalAmount)}
                                </td>
                                <td className="p-4 text-right align-middle">
                                    <div className="flex justify-end">
                                        <Button size="sm" variant="ghost" onClick={() => handleOpenModal(o)} className="text-blue-600 hover:bg-blue-50">
                                            <Eye size={18} /> Detalhes
                                        </Button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                {filteredOrders.length === 0 && <div className="p-8 text-center text-gray-500">Nenhum pedido encontrado.</div>}
            </div>

            {selectedOrder && (
                <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
                    <div className="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in duration-200">
                        <div className="p-6 border-b flex justify-between items-center sticky top-0 bg-white z-10">
                            <div>
                                <h3 className="text-xl font-bold text-gray-800">Pedido #{selectedOrder.id.slice(0,8)}</h3>
                                <p className="text-sm text-gray-500">
                                    {selectedOrder.customerName} - {selectedOrder.customerCpf}
                                </p>
                            </div>
                            <button onClick={handleCloseModal} className="text-gray-400 hover:text-gray-600"><X size={24}/></button>
                        </div>
                        
                        <div className="p-6 space-y-6">
                            <div className="grid md:grid-cols-2 gap-4 bg-gray-50 p-4 rounded border border-gray-200 text-sm">
                                <div>
                                    <span className="block font-bold text-gray-600 text-xs uppercase mb-1">Cliente</span>
                                    <div className="font-semibold">{selectedOrder.customerName}</div>
                                    <div>{selectedOrder.customerEmail}</div>
                                    <div>{selectedOrder.customerCpf}</div>
                                </div>
                                <div>
                                    <span className="block font-bold text-gray-600 text-xs uppercase mb-1">Entrega</span>
                                    <div className="text-gray-700 whitespace-pre-wrap leading-tight">{selectedOrder.shippingAddress}</div>
                                </div>
                            </div>

                            <div>
                                <h4 className="font-bold text-sm text-gray-500 uppercase mb-3">Itens do Pedido</h4>
                                <div className="bg-gray-50 rounded-lg p-4 space-y-2 border border-gray-100">
                                    {selectedOrder.items.map((item, idx) => (
                                        <div key={idx} className="flex justify-between text-sm">
                                            <span>{item.quantity}x {item.productName}</span>
                                            <span className="font-medium">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.total)}</span>
                                        </div>
                                    ))}
                                    <div className="pt-2 mt-2 border-t flex justify-between font-bold text-gray-800">
                                        <span>Total</span>
                                        <span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(selectedOrder.totalAmount)}</span>
                                    </div>
                                </div>
                            </div>

                            <form onSubmit={handleUpdateOrder} className="bg-blue-50 p-5 rounded-xl border border-blue-100">
                                <h4 className="font-bold text-blue-800 mb-4 flex items-center gap-2"><Settings size={18}/> Gerenciar Pedido</h4>
                                
                                <div className="grid gap-4 mb-4">
                                    <div>
                                        <label className="block text-xs font-bold text-gray-600 mb-1">Status</label>
                                        <select 
                                            className="w-full border border-gray-300 rounded p-2 text-sm outline-none focus:border-blue-500"
                                            value={statusInput}
                                            onChange={e => setStatusInput(e.target.value)}
                                        >
                                            <option value="Pendente">Pendente</option>
                                            <option value="Pago">Pago</option>
                                            <option value="Enviado">Enviado</option>
                                            <option value="Entregue">Entregue</option>
                                            <option value="Cancelado">Cancelado</option>
                                            <option value="Reembolso Solicitado">Reembolso Solicitado</option>
                                            <option value="Aguardando Devolução">Aguardando Devolução</option>
                                            <option value="Reembolsado">Reembolsado</option>
                                        </select>
                                    </div>

                                    {(statusInput === 'Reembolsado' || statusInput === 'Cancelado') && (
                                        <div className="bg-orange-100 border border-orange-300 text-orange-800 p-3 rounded text-sm flex gap-2 items-start">
                                            <RefreshCcw size={18} className="mt-0.5 shrink-0"/>
                                            <div>
                                                <strong>Atenção:</strong> Ao salvar como "{statusInput}", o sistema tentará processar o estorno 
                                                automaticamente no Stripe (se o pagamento foi feito por lá).
                                            </div>
                                        </div>
                                    )}
                                    
                                    <div>
                                        <label className="block text-xs font-bold text-gray-600 mb-1">Código de Rastreio (Envio)</label>
                                        <input 
                                            className="w-full border border-gray-300 rounded p-2 text-sm outline-none focus:border-blue-500"
                                            placeholder="Ex: AA123456789BR"
                                            value={trackingInput}
                                            onChange={e => setTrackingInput(e.target.value)}
                                        />
                                    </div>

                                    {statusInput === 'Aguardando Devolução' && (
                                        <div className="bg-white p-4 rounded border border-gray-200 space-y-3 animate-in fade-in">
                                            <h5 className="font-bold text-xs text-gray-500 uppercase border-b pb-1 mb-2">Dados de Devolução</h5>
                                            <div>
                                                <label className="block text-xs font-bold text-gray-600 mb-1">Código de Postagem (Reverso)</label>
                                                <input 
                                                    className="w-full border border-gray-300 rounded p-2 text-sm outline-none focus:border-blue-500"
                                                    placeholder="Código dos Correios para o cliente devolver"
                                                    value={reverseCodeInput}
                                                    onChange={e => setReverseCodeInput(e.target.value)}
                                                />
                                            </div>
                                            <div>
                                                <label className="block text-xs font-bold text-gray-600 mb-1">Instruções para o Cliente</label>
                                                <textarea 
                                                    className="w-full border border-gray-300 rounded p-2 text-sm outline-none focus:border-blue-500 h-20 resize-none"
                                                    placeholder="Ex: Leve à agência com este código..."
                                                    value={returnInstructionsInput}
                                                    onChange={e => setReturnInstructionsInput(e.target.value)}
                                                />
                                            </div>
                                        </div>
                                    )}

                                    {statusInput === 'Reembolso Reprovado' && (
                                        <div className="bg-red-50 p-4 rounded border border-red-200 space-y-3 animate-in fade-in mt-3">
                                            <h5 className="font-bold text-xs text-red-800 uppercase border-b border-red-200 pb-1 mb-2">
                                                Motivo da Reprovação
                                            </h5>
                                            
                                            <div>
                                                <label className="block text-xs font-bold text-red-700 mb-1">Justificativa (Obrigatório)</label>
                                                <textarea 
                                                    className="w-full border border-red-300 rounded p-2 text-sm outline-none focus:border-red-500 h-24 resize-none"
                                                    placeholder="Explique ao cliente por que o reembolso foi negado..."
                                                    value={refundReason}
                                                    onChange={e => setRefundReason(e.target.value)}
                                                    required={statusInput === 'Reembolso Reprovado'}
                                                />
                                            </div>

                                            <div>
                                                <label className="block text-xs font-bold text-red-700 mb-1">Prova / Evidência (Opcional)</label>
                                                <div className="flex gap-2 items-center">
                                                    <input 
                                                        type="file" 
                                                        accept="image/*,video/mp4,video/webm"
                                                        onChange={handleProofUpload}
                                                        className="text-xs w-full file:mr-2 file:py-1 file:px-2 file:rounded file:border-0 file:bg-red-100 file:text-red-700 hover:file:bg-red-200"
                                                    />
                                                    {uploadingProof && <span className="text-xs text-gray-500">Enviando...</span>}
                                                </div>
                                                {refundProof && (
                                                    <div className="mt-2 text-xs text-green-600 truncate">
                                                        Arquivo anexado: <a href={refundProof} target="_blank" rel="noopener noreferrer" className="underline">Ver arquivo</a>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                </div>

                                <div className="flex justify-end gap-3">
                                    <Button type="button" variant="ghost" onClick={handleCloseModal}>Cancelar</Button>
                                    <Button type="submit">Salvar Alterações</Button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

const OrderStatusBadge = ({ status }) => {
    const styles = {
        'Pendente': 'bg-yellow-100 text-yellow-800',
        'Pago': 'bg-indigo-100 text-indigo-800',
        'Enviado': 'bg-blue-100 text-blue-800',
        'Entregue': 'bg-green-100 text-green-800',
        'Cancelado': 'bg-red-100 text-red-800',
        'Reembolso Solicitado': 'bg-purple-100 text-purple-800',
        'Aguardando Devolução': 'bg-orange-100 text-orange-800',
        'Reembolsado': 'bg-gray-800 text-white',
        'Reembolso Reprovado': 'bg-red-200 text-red-900'
    };
    return (
        <span className={`px-2 py-1 rounded text-xs font-bold ${styles[status] || 'bg-gray-100'}`}>
            {status}
        </span>
    );
};

export default OrdersTab;