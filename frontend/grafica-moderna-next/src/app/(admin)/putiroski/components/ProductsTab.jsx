import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { Search, ArrowUp, ArrowDown, ArrowUpDown, Edit, Trash2, Box } from 'lucide-react';
import { ProductService } from '@/app/(website)/(shop)/services/productService';
import { Button } from '@/app/(website)/components/ui/Button';

const ProductsTab = () => {
  const [products, setProducts] = useState([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const pageSize = 10;
  
  const [sortConfig, setSortConfig] = useState({ key: 'createdAt', direction: 'desc' });
  const [searchTerm, setSearchTerm] = useState('');

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

  useEffect(() => { loadProducts(); }, [sortConfig, searchTerm, currentPage]);

  const loadProducts = async () => {
    try {
        setLoading(true);
        const data = await ProductService.getAll(currentPage, pageSize, searchTerm, sortConfig.key, sortConfig.direction);

        setProducts(data.items);
        const total = Math.ceil(data.totalCount / pageSize); 
        setTotalPages(total || 1); 
    } catch (e) { 
        toast.error("Erro ao carregar produtos"); 
    } finally {
        setLoading(false);
    }
};

  const handleSort = (key) => {
      let direction = 'asc';
      if (sortConfig.key === key && sortConfig.direction === 'asc') {
          direction = 'desc';
      }
      setSortConfig({ key, direction });
  };

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
          
          const formattedPrice = parseFloat(price.toString().replace(',', '.'));

        if (isNaN(formattedPrice)) {
            toast.error("Preço inválido");
            setLoading(false);
            return;
        }

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
      setPrice(p?.price ? p.price.toString().replace('.', ',') : '');
      setStock(p?.stockQuantity || '');
      setWeight(p?.weight || '');
      setWidth(p?.width || '');
      setHeight(p?.height || '');
      setLength(p?.length || '');
      setImageFile(null);
      setIsModalOpen(true);
  };

  const handlePriceChange = (e) => {
    let val = e.target.value;
    val = val.replace(/[^0-9.,]/g, '');
    const parts = val.split(/[,.]/);
    if (parts.length > 2) return; 
    setPrice(val);
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
            <div className="p-4 border-t border-gray-100 flex items-center justify-between bg-gray-50">
                <span className="text-sm text-gray-500">
                    Página <strong>{currentPage}</strong> de <strong>{totalPages}</strong>
                </span>
                <div className="flex gap-2">
                    <button 
                        onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                        disabled={currentPage === 1}
                        className="px-3 py-1 border rounded hover:bg-white disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium text-gray-600"
                    >
                        Anterior
                    </button>
                    <button 
                        onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                        disabled={currentPage === totalPages}
                        className="px-3 py-1 border rounded hover:bg-white disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium text-gray-600"
                    >
                        Próxima
                    </button>
                </div>
            </div>
          </div>

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
                            <input 
                                className="w-full border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" 
                                type="text" 
                                placeholder="0,00"
                                value={price} 
                                onChange={handlePriceChange} 
                                required 
                            />
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

export default ProductsTab;