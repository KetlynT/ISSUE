import React, { useEffect, useState } from 'react';
import { ProductService } from '../services/productService';
import { ContentService } from '../services/contentService';
import { AuthService } from '../services/authService';
import { Button } from '../components/ui/Button';
import toast from 'react-hot-toast';
import { 
  LogOut, Plus, Edit, Trash2, Package, Search, Image as ImageIcon,
  Settings, FileText, Save, Layout
} from 'lucide-react';

export const AdminDashboard = () => {
  const [activeTab, setActiveTab] = useState('products'); // 'products', 'settings', 'pages'

  return (
    <div className="min-h-screen bg-gray-50 font-sans">
      {/* Navbar Admin */}
      <nav className="bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center sticky top-0 z-30 shadow-sm">
        <div className="flex items-center gap-2">
           <div className="bg-blue-600 p-2 rounded-lg text-white font-bold">GM</div>
           <h1 className="text-xl font-bold text-gray-800">Painel Administrativo</h1>
        </div>
        <button 
          onClick={AuthService.logout} 
          className="flex items-center gap-2 text-gray-500 hover:text-red-600 transition-colors font-medium text-sm"
        >
          <LogOut size={18} />
          Sair
        </button>
      </nav>

      <div className="max-w-7xl mx-auto p-6 lg:p-10">
        
        {/* Navegação por Abas */}
        <div className="flex gap-4 mb-8 border-b border-gray-200 pb-1 overflow-x-auto">
            <TabButton active={activeTab === 'products'} onClick={() => setActiveTab('products')} icon={<Package size={18} />}>
                Produtos
            </TabButton>
            <TabButton active={activeTab === 'settings'} onClick={() => setActiveTab('settings')} icon={<Settings size={18} />}>
                Configurações Gerais
            </TabButton>
            <TabButton active={activeTab === 'pages'} onClick={() => setActiveTab('pages')} icon={<FileText size={18} />}>
                Páginas de Texto
            </TabButton>
        </div>

        {/* Conteúdo das Abas */}
        <div className="animate-in fade-in duration-300">
            {activeTab === 'products' && <ProductsTab />}
            {activeTab === 'settings' && <SettingsTab />}
            {activeTab === 'pages' && <PagesTab />}
        </div>
      </div>
    </div>
  );
};

// --- COMPONENTES AUXILIARES ---

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

const InputGroup = ({ label, name, value, onChange }) => (
    <div>
        <label className="block text-sm font-semibold text-gray-700 mb-1">{label}</label>
        <input 
            type="text" name={name} value={value || ''} onChange={onChange}
            className="w-full border border-gray-300 px-4 py-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-colors"
        />
    </div>
);

// --- ABA 1: PRODUTOS (Lógica completa) ---
const ProductsTab = () => {
  const [products, setProducts] = useState([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [loading, setLoading] = useState(true);

  // Estados do Formulário
  const [name, setName] = useState('');
  const [desc, setDesc] = useState('');
  const [price, setPrice] = useState('');
  const [imageFile, setImageFile] = useState(null);
  const [imageUrl, setImageUrl] = useState('');

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      const data = await ProductService.getAll();
      setProducts(data);
    } catch (error) {
      toast.error("Erro ao carregar produtos.");
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (e) => {
    e.preventDefault();
    
    const savePromise = (async () => {
      let finalImageUrl = imageUrl;
      if (imageFile) {
        finalImageUrl = await ProductService.uploadImage(imageFile);
      }

      const productData = {
        name,
        description: desc,
        price: parseFloat(price),
        imageUrl: finalImageUrl
      };

      if (editingProduct) {
        await ProductService.update(editingProduct.id, productData);
      } else {
        await ProductService.create(productData);
      }

      closeModal();
      await loadProducts();
    })();

    toast.promise(savePromise, {
      loading: 'Salvando...',
      success: 'Produto salvo com sucesso!',
      error: 'Erro ao salvar produto.',
    });
  };

  const handleDelete = async (id) => {
    toast((t) => (
      <div className="flex flex-col gap-2">
        <span className="font-medium">Tem certeza que deseja excluir?</span>
        <div className="flex gap-2">
          <button
            onClick={() => { toast.dismiss(t.id); performDelete(id); }}
            className="bg-red-500 text-white px-3 py-1 rounded text-sm hover:bg-red-600"
          >
            Sim
          </button>
          <button
            onClick={() => toast.dismiss(t.id)}
            className="bg-gray-200 text-gray-800 px-3 py-1 rounded text-sm hover:bg-gray-300"
          >
            Cancelar
          </button>
        </div>
      </div>
    ));
  };

  const performDelete = async (id) => {
    const deletePromise = (async () => {
      await ProductService.delete(id);
      await loadProducts();
    })();
    toast.promise(deletePromise, { loading: 'Excluindo...', success: 'Excluído!', error: 'Erro.' });
  };

  const openModal = (product = null) => {
    if (product) {
      setEditingProduct(product);
      setName(product.name);
      setDesc(product.description);
      setPrice(product.price);
      setImageUrl(product.imageUrl);
    } else {
      setEditingProduct(null);
      setName('');
      setDesc('');
      setPrice('');
      setImageUrl('');
    }
    setImageFile(null);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingProduct(null);
  };

  return (
    <>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Catálogo de Produtos</h2>
        <Button onClick={() => openModal()} className="rounded-full shadow-blue-500/20">
          <Plus size={20} /> Novo Produto
        </Button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        {loading ? <div className="p-10 text-center">Carregando...</div> : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-100">
              <thead className="bg-gray-50/50">
                <tr>
                  <th className="px-6 py-4 text-left text-xs font-semibold text-gray-500 uppercase">Produto</th>
                  <th className="px-6 py-4 text-left text-xs font-semibold text-gray-500 uppercase">Preço</th>
                  <th className="px-6 py-4 text-right text-xs font-semibold text-gray-500 uppercase">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {products.map((prod) => (
                  <tr key={prod.id} className="hover:bg-blue-50/30 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="h-10 w-10 flex-shrink-0 bg-gray-100 rounded-lg overflow-hidden border border-gray-200">
                          <img className="h-full w-full object-cover" src={prod.imageUrl || 'https://via.placeholder.com/100'} alt={prod.name} />
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-bold text-gray-900">{prod.name}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600 font-medium">
                      {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(prod.price)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end gap-2">
                        <button onClick={() => openModal(prod)} className="p-2 text-blue-600 hover:bg-blue-100 rounded-full transition-colors"><Edit size={18} /></button>
                        <button onClick={() => handleDelete(prod.id)} className="p-2 text-red-500 hover:bg-red-100 rounded-full transition-colors"><Trash2 size={18} /></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {isModalOpen && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl p-8 max-w-lg w-full shadow-2xl">
            <h3 className="text-xl font-bold mb-6 text-gray-800">
              {editingProduct ? 'Editar Produto' : 'Novo Produto'}
            </h3>
            <form onSubmit={handleSave} className="space-y-5">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">Nome</label>
                <input type="text" className="w-full border p-2 rounded-lg" value={name} onChange={e => setName(e.target.value)} required />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">Descrição</label>
                <textarea className="w-full border p-2 rounded-lg h-20 resize-none" value={desc} onChange={e => setDesc(e.target.value)} />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1">Preço (R$)</label>
                <input type="number" step="0.01" className="w-full border p-2 rounded-lg" value={price} onChange={e => setPrice(e.target.value)} required />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">Imagem</label>
                <div className="border-2 border-dashed border-gray-300 rounded-lg p-4 text-center cursor-pointer relative">
                    <input type="file" className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" onChange={e => setImageFile(e.target.files[0])} accept="image/*" />
                    <span className="text-sm text-gray-500">{imageFile ? imageFile.name : "Clique para upload"}</span>
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-8 pt-4 border-t border-gray-100">
                <Button type="button" variant="ghost" onClick={closeModal}>Cancelar</Button>
                <Button type="submit">Salvar</Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
};

// --- ABA 2: CONFIGURAÇÕES GERAIS ---
const SettingsTab = () => {
    const [formData, setFormData] = useState({});
    const [loading, setLoading] = useState(true);
    const [heroImageFile, setHeroImageFile] = useState(null);

    useEffect(() => {
        load();
    }, []);

    const load = async () => {
        const data = await ContentService.getSettings();
        setFormData(data);
        setLoading(false);
    };

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSave = async (e) => {
        e.preventDefault();
        
        const savePromise = (async () => {
            let updatedData = { ...formData };

            // Se houver nova imagem de background selecionada, faz upload
            if (heroImageFile) {
                const url = await ProductService.uploadImage(heroImageFile); // Reutilizando serviço de upload
                updatedData.hero_bg_url = url;
            }

            await ContentService.saveSettings(updatedData);
            // Atualiza estado local
            setFormData(updatedData);
            setHeroImageFile(null);
        })();

        toast.promise(savePromise, {
            loading: 'Salvando...',
            success: 'Configurações atualizadas!',
            error: 'Erro ao salvar.'
        });
    };

    if(loading) return <div>Carregando...</div>;

    return (
        <form onSubmit={handleSave} className="bg-white p-8 rounded-xl shadow-sm border border-gray-100 max-w-4xl mx-auto">
            <h2 className="text-xl font-bold mb-6 flex items-center gap-2 pb-2 border-b"><Layout className="text-blue-600"/> Home Page (Hero)</h2>
            
            {/* Seção de Imagem de Fundo NOVO */}
            <div className="mb-8 p-4 bg-blue-50 rounded-lg border border-blue-100">
                <label className="block text-sm font-bold text-gray-800 mb-2">Imagem de Fundo do Hero</label>
                <div className="flex items-center gap-4">
                    {/* Preview da imagem atual ou nova */}
                    <div className="w-24 h-24 bg-gray-200 rounded-lg overflow-hidden border border-gray-300 relative">
                        {(heroImageFile ? URL.createObjectURL(heroImageFile) : formData.hero_bg_url) ? (
                            <img 
                                src={heroImageFile ? URL.createObjectURL(heroImageFile) : formData.hero_bg_url} 
                                className="w-full h-full object-cover" 
                                alt="Hero Bg" 
                            />
                        ) : (
                            <div className="flex items-center justify-center h-full text-xs text-gray-500">Sem imagem</div>
                        )}
                    </div>
                    
                    <div className="flex-1">
                        <input 
                            type="file" 
                            className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-100 file:text-blue-700 hover:file:bg-blue-200"
                            onChange={e => setHeroImageFile(e.target.files[0])}
                            accept="image/*"
                        />
                        <p className="text-xs text-gray-500 mt-1">Recomendado: 1920x1080px (JPG/PNG)</p>
                    </div>
                </div>
            </div>

            <div className="grid md:grid-cols-2 gap-6 mb-8">
                <InputGroup label="Badge (Topo)" name="hero_badge" value={formData.hero_badge} onChange={handleChange} />
                <InputGroup label="Título Principal" name="hero_title" value={formData.hero_title} onChange={handleChange} />
                <div className="md:col-span-2">
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Subtítulo</label>
                    <textarea name="hero_subtitle" value={formData.hero_subtitle || ''} onChange={handleChange} className="w-full border border-gray-300 rounded-lg p-2 h-20 outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
            </div>

            <h2 className="text-xl font-bold mb-6 pt-6 flex items-center gap-2 pb-2 border-b"><Settings className="text-blue-600"/> Contato & Rodapé</h2>
            <div className="grid md:grid-cols-2 gap-6">
                <InputGroup label="WhatsApp (Apenas números)" name="whatsapp_number" value={formData.whatsapp_number} onChange={handleChange} />
                <InputGroup label="WhatsApp (Exibição)" name="whatsapp_display" value={formData.whatsapp_display} onChange={handleChange} />
                <InputGroup label="E-mail de Contato" name="contact_email" value={formData.contact_email} onChange={handleChange} />
                <InputGroup label="Endereço Físico" name="address" value={formData.address} onChange={handleChange} />
            </div>

            <div className="mt-8 flex justify-end">
                <Button type="submit"><Save size={18}/> Salvar Alterações</Button>
            </div>
        </form>
    );
};

// --- ABA 3: PÁGINAS DE TEXTO ---
const PagesTab = () => {
    const [pages, setPages] = useState([]);
    const [selectedPage, setSelectedPage] = useState(null);

    useEffect(() => {
        ContentService.getAllPages().then(setPages);
    }, []);

    const handleEdit = async (page) => {
        const fullPage = await ContentService.getPage(page.slug);
        setSelectedPage(fullPage);
    };

    const handleSave = async (e) => {
        e.preventDefault();
        await ContentService.updatePage(selectedPage.id, selectedPage);
        toast.success("Página atualizada!");
        setSelectedPage(null);
    };

    return (
        <div className="grid md:grid-cols-3 gap-8 h-full">
            {/* Lista Lateral */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden h-fit">
                <div className="p-4 bg-gray-50 border-b font-bold text-gray-700">Páginas Disponíveis</div>
                <div className="divide-y divide-gray-100">
                    {pages.map(p => (
                        <button 
                            key={p.id} 
                            onClick={() => handleEdit(p)}
                            className={`w-full text-left p-4 hover:bg-blue-50 transition-colors ${selectedPage?.id === p.id ? 'bg-blue-50 text-blue-700 font-bold border-l-4 border-blue-600' : ''}`}
                        >
                            {p.title} <span className="block text-xs text-gray-400 font-normal">/{p.slug}</span>
                        </button>
                    ))}
                </div>
            </div>

            {/* Área de Edição */}
            <div className="md:col-span-2">
                {selectedPage ? (
                    <form onSubmit={handleSave} className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                        <h3 className="text-lg font-bold mb-4 border-b pb-2">Editando: {selectedPage.title}</h3>
                        <div className="mb-4">
                            <label className="block text-sm font-bold mb-2">Título da Página</label>
                            <input 
                                className="w-full border border-gray-300 p-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none" 
                                value={selectedPage.title} 
                                onChange={e => setSelectedPage({...selectedPage, title: e.target.value})}
                            />
                        </div>
                        <div className="mb-4">
                            <label className="block text-sm font-bold mb-2">Conteúdo (HTML)</label>
                            <textarea 
                                className="w-full border border-gray-300 p-3 rounded-lg h-96 font-mono text-sm focus:ring-2 focus:ring-blue-500 outline-none" 
                                value={selectedPage.content} 
                                onChange={e => setSelectedPage({...selectedPage, content: e.target.value})}
                            />
                            <p className="text-xs text-gray-500 mt-2 bg-yellow-50 p-2 rounded border border-yellow-100">
                                💡 Dica: Use tags HTML simples como &lt;p&gt;, &lt;h2&gt;, &lt;strong&gt;, &lt;ul&gt;, &lt;li&gt;.
                            </p>
                        </div>
                        <div className="flex gap-2 justify-end">
                            <Button type="button" variant="ghost" onClick={() => setSelectedPage(null)}>Cancelar</Button>
                            <Button type="submit">Salvar Página</Button>
                        </div>
                    </form>
                ) : (
                    <div className="bg-white p-12 rounded-xl border-2 border-dashed border-gray-200 text-center text-gray-400 h-64 flex flex-col items-center justify-center">
                        <FileText size={48} className="mb-2 opacity-20" />
                        Selecione uma página ao lado para editar o conteúdo.
                    </div>
                )}
            </div>
        </div>
    );
};