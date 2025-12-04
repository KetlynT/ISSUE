import React, { useEffect, useState } from 'react';
import { ProductService } from '../services/productService';
import { ContentService } from '../services/contentService';
import { AuthService } from '../services/authService';
import { CartService } from '../services/cartService';
import { DashboardService } from '../services/dashboardService';
import { CouponService } from '../services/couponService';
import { Button } from '../components/ui/Button';
import toast from 'react-hot-toast';
import { 
  LogOut, Edit, Trash2, Package, Settings, FileText, 
  Box, Truck, BarChart2, AlertTriangle, DollarSign, 
  ShoppingBag, Tag, Search, Eye, X, RefreshCcw, ArrowUp, ArrowDown, ArrowUpDown 
} from 'lucide-react';

import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';

export const AdminDashboard = () => {
  const [activeTab, setActiveTab] = useState('overview'); 

  return (
    <div className="min-h-screen bg-gray-50 font-sans">
      <nav className="bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center sticky top-0 z-30 shadow-sm">
        <div className="flex items-center gap-2">
           <div className="bg-blue-600 p-2 rounded-lg text-white font-bold">GM</div>
           <h1 className="text-xl font-bold text-gray-800">Painel Restrito</h1>
        </div>
        <div className="flex items-center gap-4">
            <a href="/" className="text-sm text-blue-600 hover:underline" target="_blank" rel="noopener noreferrer">Ver Site</a>
            <button 
            onClick={AuthService.logout} 
            className="flex items-center gap-2 text-gray-500 hover:text-red-600 transition-colors font-medium text-sm"
            >
            <LogOut size={18} /> Sair
            </button>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto p-6 lg:p-10">
        <div className="flex gap-4 mb-8 border-b border-gray-200 pb-1 overflow-x-auto">
            <TabButton active={activeTab === 'overview'} onClick={() => setActiveTab('overview')} icon={<BarChart2 size={18} />}>
                Visão Geral
            </TabButton>
            <TabButton active={activeTab === 'orders'} onClick={() => setActiveTab('orders')} icon={<Truck size={18} />}>
                Pedidos
            </TabButton>
            <TabButton active={activeTab === 'products'} onClick={() => setActiveTab('products')} icon={<Package size={18} />}>
                Produtos
            </TabButton>
            <TabButton active={activeTab === 'coupons'} onClick={() => setActiveTab('coupons')} icon={<Tag size={18} />}>
                Cupons
            </TabButton>
            <TabButton active={activeTab === 'settings'} onClick={() => setActiveTab('settings')} icon={<Settings size={18} />}>
                Configurações
            </TabButton>
            <TabButton active={activeTab === 'pages'} onClick={() => setActiveTab('pages')} icon={<FileText size={18} />}>
                Páginas
            </TabButton>
        </div>

        <div className="animate-in fade-in duration-300">
            {activeTab === 'overview' && <OverviewTab />}
            {activeTab === 'products' && <ProductsTab />}
            {activeTab === 'orders' && <OrdersTab />}
            {activeTab === 'coupons' && <CouponsTab />}
            {activeTab === 'settings' && <SettingsTab />}
            {activeTab === 'pages' && <PagesTab />}
        </div>
      </div>
    </div>
  );
};

// Componentes Auxiliares
const TabButton = ({ active, onClick, children, icon }) => (
    <button 
        onClick={onClick}
        className={`flex items-center gap-2 px-6 py-3 font-medium transition-all rounded-t-lg whitespace-nowrap ${
            active 
            ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50/50' 
            : 'text-gray-500 hover:text-gray-700 hover:bg-gray-100'
        }`}
    >
        {icon} {children}
    </button>
);

const InputGroup = ({ label, name, value, onChange, type = "text", placeholder }) => (
    <div>
        <label className="block text-sm font-semibold text-gray-700 mb-1">{label}</label>
        <input 
            type={type} name={name} value={value || ''} onChange={onChange} placeholder={placeholder}
            className="w-full border border-gray-300 px-4 py-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-colors"
        />
    </div>
);

// --- ABA: VISÃO GERAL (Atualizada com Reembolso) ---
const OverviewTab = () => {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        DashboardService.getStats()
            .then(setStats)
            .catch(() => toast.error("Erro ao carregar estatísticas"))
            .finally(() => setLoading(false));
    }, []);

    if (loading) return <div className="text-center py-10">Carregando painel...</div>;
    if (!stats) return null;

    return (
        <div className="space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Receita Líquida</div>
                        <div className="p-2 bg-green-100 text-green-600 rounded-lg"><DollarSign size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">
                        {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(stats.totalRevenue)}
                    </div>
                </div>

                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Total Reembolsado</div>
                        <div className="p-2 bg-purple-100 text-purple-600 rounded-lg"><RefreshCcw size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">
                        {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(stats.totalRefunded || 0)}
                    </div>
                </div>
                
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Pedidos Totais</div>
                        <div className="p-2 bg-blue-100 text-blue-600 rounded-lg"><ShoppingBag size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">{stats.totalOrders}</div>
                    <div className="text-xs text-gray-500 mt-1">{stats.pendingOrders} pendentes de envio</div>
                </div>

                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Alerta de Estoque</div>
                        <div className="p-2 bg-red-100 text-red-600 rounded-lg"><AlertTriangle size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">{stats.lowStockProducts.length}</div>
                    <div className="text-xs text-gray-500 mt-1">Itens críticos</div>
                </div>
            </div>
        </div>
    );
};

// --- ABA: PRODUTOS (Com Filtro e Ordenação) ---
const ProductsTab = () => {
  const [products, setProducts] = useState([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  
  // Estado para filtros
  const [sortConfig, setSortConfig] = useState({ key: 'createdAt', direction: 'desc' });
  const [searchTerm, setSearchTerm] = useState('');

  // Formulário
  const [name, setName] = useState('');
  const [desc, setDesc] = useState('');
  const [price, setPrice] = useState('');
  const [stock, setStock] = useState('');
  const [imageFile, setImageFile] = useState(null);
  const [weight, setWeight] = useState('');
  const [width, setWidth] = useState('');
  const [height, setHeight] = useState('');
  const [length, setLength] = useState('');

  const [loading, setLoading] = useState(false);

  useEffect(() => { loadProducts(); }, [sortConfig, searchTerm]);

  const loadProducts = async () => {
      try {
          // Passa os parâmetros de ordenação para a API (O backend já suporta search/sort/order)
          const data = await ProductService.getAll(1, 100, searchTerm, sortConfig.key, sortConfig.direction);
          setProducts(data.items);
      } catch (e) { toast.error("Erro ao carregar produtos"); }
  };

  const handleSort = (key) => {
      let direction = 'asc';
      if (sortConfig.key === key && sortConfig.direction === 'asc') {
          direction = 'desc';
      }
      setSortConfig({ key, direction });
  };

  // Ícone de ordenação
  const SortIcon = ({ column }) => {
      if (sortConfig.key !== column) return <ArrowUpDown size={14} className="text-gray-300 ml-1 inline" />;
      return sortConfig.direction === 'asc' 
          ? <ArrowUp size={14} className="text-blue-600 ml-1 inline" /> 
          : <ArrowDown size={14} className="text-blue-600 ml-1 inline" />;
  };

  const handleSave = async (e) => {
      e.preventDefault();
      setLoading(true);
      try {
          let imageUrl = editingProduct?.imageUrl || '';
          if(imageFile) imageUrl = await ProductService.uploadImage(imageFile);

          const data = { 
              name, 
              description: desc, 
              price: parseFloat(price), 
              imageUrl,
              stockQuantity: parseInt(stock),
              weight: parseFloat(weight),
              width: parseInt(width),
              height: parseInt(height),
              length: parseInt(length)
          };
          
          if(editingProduct) await ProductService.update(editingProduct.id, data);
          else await ProductService.create(data);

          setIsModalOpen(false);
          setImageFile(null);
          loadProducts();
          toast.success("Salvo com sucesso!");
      } catch (e) { 
          toast.error("Erro ao salvar. Verifique os campos."); 
      } finally {
          setLoading(false);
      }
  };

  const handleDelete = async (id) => {
      if(!window.confirm("Tem certeza que deseja excluir este produto?")) return;
      try {
          await ProductService.delete(id);
          loadProducts();
          toast.success("Excluído!");
      } catch(e) { toast.error("Erro ao excluir"); }
  };

  const openModal = (p = null) => {
      setEditingProduct(p);
      setName(p?.name || '');
      setDesc(p?.description || '');
      setPrice(p?.price || '');
      setStock(p?.stockQuantity || '');
      setWeight(p?.weight || '');
      setWidth(p?.width || '');
      setHeight(p?.height || '');
      setLength(p?.length || '');
      setImageFile(null);
      setIsModalOpen(true);
  };

  return (
      <div>
          <div className="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
              <h2 className="text-xl font-bold text-gray-800">Catálogo de Produtos</h2>
              <div className="flex gap-2 w-full md:w-auto">
                  <div className="relative flex-grow">
                      <Search className="absolute left-3 top-2.5 text-gray-400" size={18} />
                      <input 
                          className="pl-10 pr-4 py-2 border rounded-lg w-full md:w-64 focus:ring-2 focus:ring-blue-500 outline-none text-sm"
                          placeholder="Buscar por nome..."
                          value={searchTerm}
                          onChange={e => setSearchTerm(e.target.value)}
                      />
                  </div>
                  <Button onClick={() => openModal()} size="sm">+ Novo</Button>
              </div>
          </div>

          <div className="bg-white rounded-lg shadow border overflow-hidden">
              <table className="w-full text-left">
                  <thead className="bg-gray-50 border-b text-gray-600 text-sm uppercase font-bold">
                      <tr>
                          <th className="p-4 cursor-pointer hover:bg-gray-100 transition-colors" onClick={() => handleSort('name')}>
                              Produto <SortIcon column="name" />
                          </th>
                          <th className="p-4 text-center cursor-pointer hover:bg-gray-100 transition-colors" onClick={() => handleSort('stockQuantity')}>
                              Estoque <SortIcon column="stockQuantity" />
                          </th>
                          <th className="p-4 cursor-pointer hover:bg-gray-100 transition-colors" onClick={() => handleSort('price')}>
                              Preço <SortIcon column="price" />
                          </th>
                          <th className="p-4 text-right">Ações</th>
                      </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                      {products.map(p => (
                          <tr key={p.id} className="hover:bg-gray-50 transition-colors">
                              <td className="p-4 align-middle">
                                  <div className="font-bold text-gray-800">{p.name}</div>
                                  <div className="text-xs text-gray-500 truncate max-w-xs">{p.description}</div>
                              </td>
                              <td className="p-4 text-center align-middle">
                                  <span className={`px-2 py-1 rounded text-xs font-bold ${p.stockQuantity < 10 ? 'bg-red-100 text-red-700' : 'bg-gray-100 text-gray-700'}`}>
                                    {p.stockQuantity} un
                                  </span>
                              </td>
                              <td className="p-4 align-middle font-mono text-blue-600">R$ {p.price.toFixed(2)}</td>
                              <td className="p-4 text-right align-middle">
                                  <div className="flex justify-end gap-2">
                                    <button onClick={() => openModal(p)} className="text-blue-600 hover:bg-blue-50 p-2 rounded"><Edit size={18}/></button>
                                    <button onClick={() => handleDelete(p.id)} className="text-red-600 hover:bg-red-50 p-2 rounded"><Trash2 size={18}/></button>
                                  </div>
                              </td>
                          </tr>
                      ))}
                  </tbody>
              </table>
          </div>

          {/* Modal de Produto (Omitido para não repetir código, mantém o mesmo) */}
          {isModalOpen && (
              <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 overflow-y-auto py-10">
                  <form onSubmit={handleSave} className="bg-white p-8 rounded-xl w-full max-w-2xl space-y-5 shadow-2xl animate-in fade-in zoom-in duration-200 my-auto">
                      <h3 className="font-bold text-2xl text-gray-800 border-b pb-2">{editingProduct ? 'Editar Produto' : 'Novo Produto'}</h3>
                      
                      <div className="grid md:grid-cols-2 gap-4">
                        <div className="md:col-span-2">
                            <label className="block text-sm font-bold text-gray-700 mb-1">Nome</label>
                            <input className="w-full border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" value={name} onChange={e=>setName(e.target.value)} required />
                        </div>
                        <div className="md:col-span-2">
                            <label className="block text-sm font-bold text-gray-700 mb-1">Descrição</label>
                            <textarea className="w-full border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none h-24 resize-none" value={desc} onChange={e=>setDesc(e.target.value)} />
                        </div>
                        <div>
                            <label className="block text-sm font-bold text-gray-700 mb-1">Preço (R$)</label>
                            <input className="w-full border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" type="number" step="0.01" value={price} onChange={e=>setPrice(e.target.value)} required />
                        </div>
                        <div>
                            <label className="block text-sm font-bold text-gray-700 mb-1">Estoque</label>
                            <input className="w-full border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" type="number" value={stock} onChange={e=>setStock(e.target.value)} required />
                        </div>
                        <div className="md:col-span-2">
                            <label className="block text-sm font-bold text-gray-700 mb-1">Imagem</label>
                            <input type="file" onChange={e=>setImageFile(e.target.files[0])} className="text-sm w-full file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100" />
                        </div>
                      </div>

                      <div className="bg-gray-50 p-4 rounded-lg border border-gray-200 mt-2">
                        <h4 className="font-bold text-sm text-gray-700 mb-3 flex items-center gap-2"><Box size={16}/> Dados para Frete</h4>
                        <div className="grid grid-cols-4 gap-4">
                            <div><label className="block text-xs font-bold text-gray-600 mb-1">Peso (kg)</label><input type="number" step="0.001" className="w-full border p-2 rounded text-sm" value={weight} onChange={e=>setWeight(e.target.value)} required /></div>
                            <div><label className="block text-xs font-bold text-gray-600 mb-1">Largura</label><input type="number" className="w-full border p-2 rounded text-sm" value={width} onChange={e=>setWidth(e.target.value)} required /></div>
                            <div><label className="block text-xs font-bold text-gray-600 mb-1">Altura</label><input type="number" className="w-full border p-2 rounded text-sm" value={height} onChange={e=>setHeight(e.target.value)} required /></div>
                            <div><label className="block text-xs font-bold text-gray-600 mb-1">Comp.</label><input type="number" className="w-full border p-2 rounded text-sm" value={length} onChange={e=>setLength(e.target.value)} required /></div>
                        </div>
                      </div>

                      <div className="flex justify-end gap-3 pt-4">
                          <Button type="button" variant="ghost" onClick={()=>setIsModalOpen(false)}>Cancelar</Button>
                          <Button type="submit" isLoading={loading}>Salvar</Button>
                      </div>
                  </form>
              </div>
          )}
      </div>
  );
};

// --- ABA: PEDIDOS (Correção de Alinhamento) ---
const OrdersTab = () => {
    const [orders, setOrders] = useState([]);
    const [filteredOrders, setFilteredOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [statusFilter, setStatusFilter] = useState('Todos');
    
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
                o.shippingAddress.toLowerCase().includes(lowerTerm)
            );
        }
        setFilteredOrders(result);
    }, [orders, statusFilter, searchTerm]);

    const loadOrders = async () => {
        try {
            const data = await CartService.getAllOrders();
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
    };

    const handleCloseModal = () => {
        setSelectedOrder(null);
    };

    const handleUpdateOrder = async (e) => {
        e.preventDefault();
        const toastId = toast.loading("Atualizando...");
        try {
            await CartService.updateOrderStatus(selectedOrder.id, {
                status: statusInput,
                trackingCode: trackingInput,
                reverseLogisticsCode: reverseCodeInput,
                returnInstructions: returnInstructionsInput
            });
            
            const updatedList = orders.map(o => o.id === selectedOrder.id ? {
                ...o, 
                status: statusInput, 
                trackingCode: trackingInput,
                reverseLogisticsCode: reverseCodeInput,
                returnInstructions: returnInstructionsInput
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
                            placeholder="Buscar ID ou Endereço..."
                            value={searchTerm}
                            onChange={e => setSearchTerm(e.target.value)}
                        />
                    </div>
                    <select 
                        className="border rounded-lg px-3 py-2 bg-white outline-none focus:ring-2 focus:ring-blue-500"
                        value={statusFilter}
                        onChange={e => setStatusFilter(e.target.value)}
                    >
                        <option value="Todos">Todos Status</option>
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
            </div>

            <div className="bg-white rounded-lg shadow border overflow-hidden">
                <table className="w-full text-left text-sm">
                    <thead className="bg-gray-50 border-b text-gray-600 uppercase">
                        <tr>
                            <th className="p-4">Pedido</th>
                            <th className="p-4">Status</th>
                            <th className="p-4">Rastreio</th>
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
                                    <OrderStatusBadge status={o.status} />
                                </td>
                                <td className="p-4 align-middle font-mono text-xs text-gray-600">
                                    {o.trackingCode || '-'}
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
                            <h3 className="text-xl font-bold text-gray-800">Pedido #{selectedOrder.id.slice(0,8)}</h3>
                            <button onClick={handleCloseModal} className="text-gray-400 hover:text-gray-600"><X size={24}/></button>
                        </div>
                        
                        <div className="p-6 space-y-6">
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
        'Reembolsado': 'bg-gray-800 text-white'
    };
    return (
        <span className={`px-2 py-1 rounded text-xs font-bold ${styles[status] || 'bg-gray-100'}`}>
            {status}
        </span>
    );
};

const CouponsTab = () => {
    const [coupons, setCoupons] = useState([]);
    const [form, setForm] = useState({ code: '', discountPercentage: '', validityDays: '30' });

    useEffect(() => { load(); }, []);

    const load = async () => {
        try {
            const data = await CouponService.getAll();
            setCoupons(data);
        } catch (e) { console.error(e); }
    };

    const handleCreate = async (e) => {
        e.preventDefault();
        try {
            await CouponService.create({ 
                ...form, 
                discountPercentage: parseFloat(form.discountPercentage),
                validityDays: parseInt(form.validityDays)
            });
            toast.success("Cupom criado!");
            setForm({ code: '', discountPercentage: '', validityDays: '30' });
            load();
        } catch (e) {
            toast.error(e.response?.data || "Erro ao criar");
        }
    };

    const handleDelete = async (id) => {
        if(!confirm("Excluir cupom?")) return;
        await CouponService.delete(id);
        load();
    };

    return (
        <div className="grid md:grid-cols-3 gap-8">
            <div className="bg-white p-6 rounded-xl shadow border border-gray-100 h-fit">
                <h3 className="font-bold text-gray-800 mb-4">Novo Cupom</h3>
                <form onSubmit={handleCreate} className="space-y-4">
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">CÓDIGO</label>
                        <input className="w-full border p-2 rounded uppercase" value={form.code} onChange={e => setForm({...form, code: e.target.value.toUpperCase()})} required placeholder="Ex: PROMO10"/>
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">Desconto (%)</label>
                        <input type="number" className="w-full border p-2 rounded" value={form.discountPercentage} onChange={e => setForm({...form, discountPercentage: e.target.value})} required placeholder="10"/>
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">Validade (Dias)</label>
                        <input type="number" className="w-full border p-2 rounded" value={form.validityDays} onChange={e => setForm({...form, validityDays: e.target.value})} required/>
                    </div>
                    <Button type="submit" className="w-full">Criar Cupom</Button>
                </form>
            </div>

            <div className="md:col-span-2 bg-white rounded-xl shadow border border-gray-100 overflow-hidden">
                <table className="w-full text-left">
                    <thead className="bg-gray-50 border-b text-gray-600 text-sm uppercase">
                        <tr>
                            <th className="p-4">Código</th>
                            <th className="p-4">Desconto</th>
                            <th className="p-4">Expira em</th>
                            <th className="p-4 text-right">Ação</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {coupons.map(c => (
                            <tr key={c.id}>
                                <td className="p-4 font-bold text-gray-800">{c.code}</td>
                                <td className="p-4 text-green-600 font-bold">{c.discountPercentage}%</td>
                                <td className="p-4 text-sm text-gray-500">{new Date(c.expiryDate).toLocaleDateString('pt-BR')}</td>
                                <td className="p-4 text-right">
                                    <button onClick={() => handleDelete(c.id)} className="text-red-500 hover:bg-red-50 p-2 rounded"><Trash2 size={18}/></button>
                                </td>
                            </tr>
                        ))}
                        {coupons.length === 0 && <tr><td colSpan="4" className="p-8 text-center text-gray-400">Nenhum cupom ativo.</td></tr>}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

const SettingsTab = () => {
    const [formData, setFormData] = useState({});
    const [heroImageFile, setHeroImageFile] = useState(null);
    const [logoFile, setLogoFile] = useState(null); 
    const [loading, setLoading] = useState(false);

    useEffect(() => { load(); }, []);

    const load = async () => {
        const data = await ContentService.getSettings();
        setFormData(data);
    };

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSave = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            let updatedData = { ...formData };
            if (heroImageFile) updatedData.hero_bg_url = await ProductService.uploadImage(heroImageFile);
            if (logoFile) updatedData.site_logo = await ProductService.uploadImage(logoFile);

            await ContentService.saveSettings(updatedData);
            toast.success("Configurações atualizadas!");
        } catch (e) {
            toast.error("Erro ao salvar configurações.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <form onSubmit={handleSave} className="bg-white p-8 rounded-xl shadow-sm border border-gray-100 max-w-4xl mx-auto space-y-8">
            <div className="grid md:grid-cols-2 gap-4">
                <InputGroup label="Nome do Negócio" name="site_name" value={formData.site_name} onChange={handleChange} placeholder="Ex: Minha Gráfica" />
                <InputGroup label="Badge (Hero)" name="hero_badge" value={formData.hero_badge} onChange={handleChange} />
                <InputGroup label="Título (Hero)" name="hero_title" value={formData.hero_title} onChange={handleChange} />
                <InputGroup label="Subtítulo (Hero)" name="hero_subtitle" value={formData.hero_subtitle} onChange={handleChange} />
                <InputGroup label="WhatsApp (Números)" name="whatsapp_number" value={formData.whatsapp_number} onChange={handleChange} />
                <InputGroup label="WhatsApp (Visível)" name="whatsapp_display" value={formData.whatsapp_display} onChange={handleChange} />
                <InputGroup label="CEP de Origem" name="sender_cep" value={formData.sender_cep} onChange={handleChange} />
                <InputGroup label="Email" name="contact_email" value={formData.contact_email} onChange={handleChange} />
            </div>
            <div className="grid md:grid-cols-2 gap-4">
                 <div className="border-2 border-dashed border-gray-300 p-4 rounded-lg text-center"><label>Logo</label><input type="file" onChange={e => setLogoFile(e.target.files[0])} className="text-xs"/></div>
                 <div className="border-2 border-dashed border-gray-300 p-4 rounded-lg text-center"><label>Hero BG</label><input type="file" onChange={e => setHeroImageFile(e.target.files[0])} className="text-xs"/></div>
            </div>
            <Button type="submit" className="w-full" isLoading={loading}>Salvar Configurações</Button>
        </form>
    );
};

const PagesTab = () => {
    const [pages, setPages] = useState([]);
    const [selectedPage, setSelectedPage] = useState(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => { ContentService.getAllPages().then(setPages); }, []);

    const handleEdit = async (page) => {
        const fullPage = await ContentService.getPage(page.slug);
        setSelectedPage(fullPage);
    };

    const handleSave = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            await ContentService.updatePage(selectedPage.id, selectedPage);
            toast.success("Página salva!");
            ContentService.getAllPages().then(setPages);
        } catch(e) {
            toast.error("Erro ao salvar página.");
        } finally {
            setLoading(false);
        }
    };

    const modules = { toolbar: [ [{ 'header': [1, 2, 3, false] }], ['bold', 'italic', 'underline'], [{'list': 'ordered'}, {'list': 'bullet'}], ['link', 'clean'] ] };

    return (
        <div className="grid md:grid-cols-3 gap-8">
            <div className="bg-white rounded-lg shadow border p-4 h-fit">
                <h3 className="font-bold border-b pb-2 mb-2 text-gray-700">Selecione uma Página</h3>
                {pages.map(p => (
                    <div key={p.id} onClick={() => handleEdit(p)} className={`p-3 cursor-pointer border-b last:border-0 rounded transition-colors ${selectedPage?.id === p.id ? 'bg-blue-50 text-blue-700 border-blue-200' : 'hover:bg-gray-50'}`}>
                        <div className="font-medium">{p.title}</div>
                        <small className="text-gray-400 block text-xs">/{p.slug}</small>
                    </div>
                ))}
            </div>
            
            <div className="md:col-span-2 bg-white rounded-lg shadow border p-6">
                {selectedPage ? (
                    <form onSubmit={handleSave} className="space-y-6">
                        <InputGroup label="Título da Página" name="title" value={selectedPage.title} onChange={e => setSelectedPage({...selectedPage, title: e.target.value})} />
                        <div>
                            <label className="block text-sm font-bold mb-2 text-gray-700">Conteúdo</label>
                            <ReactQuill theme="snow" value={selectedPage.content} onChange={(value) => setSelectedPage({...selectedPage, content: value})} modules={modules} className="h-64 mb-12" />
                        </div>
                        <div className="pt-4"><Button type="submit" isLoading={loading}>Salvar Alterações</Button></div>
                    </form>
                ) : (
                    <div className="text-center text-gray-400 py-20">Selecione uma página para editar.</div>
                )}
            </div>
        </div>
    );
};