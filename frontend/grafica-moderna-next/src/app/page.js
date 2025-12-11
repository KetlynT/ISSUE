'use client'
import { useEffect, useState } from 'react';
import { ProductService } from '../services/productService';
import { ContentService } from '../services/contentService';
import { ProductCard } from '../components/ProductCard';
import { Button } from '../components/ui/Button';
import { Search, Printer, ChevronLeft, ChevronRight, Filter } from 'lucide-react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';

export default function Home () {
  const navigate = useNavigate();
  const [products, setProducts] = useState([]);
  const [pagination, setPagination] = useState({ page: 1, totalPages: 1, totalItems: 0 });
  const [loading, setLoading] = useState(true);
  const [isFirstLoad, setIsFirstLoad] = useState(true);
  const [inputPage, setInputPage] = useState(1);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortOption, setSortOption] = useState(""); 
  
  const [settings, setSettings] = useState({
    hero_badge: 'Carregando...',
    hero_title: '...',
    hero_subtitle: '...',
    home_products_title: 'Nossos Produtos',
    home_products_subtitle: 'Confira nosso catálogo',
    whatsapp_number: '',
    hero_bg_url: '',
    purchase_enabled: 'true'
  });

  useEffect(() => {
    const timer = setTimeout(() => {
      loadProducts(1);
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm, sortOption]);

  useEffect(() => {
    ContentService.getSettings().then(data => {
      if (data) setSettings(prev => ({...prev, ...data}));
    }).catch(err => {
        console.error(err);
        navigate('/error', { replace: true }); 
      });
  }, [navigate]);

  const loadProducts = async (page) => {
    setLoading(true);
    try {
      let sort = '', order = '';
      if (sortOption) {
        [sort, order] = sortOption.split('-');
      }

      const data = await ProductService.getAll(page, 8, searchTerm, sort, order);
      
      setProducts(data.items);
      setPagination({
        page: data.page,
        totalPages: data.totalPages,
        totalItems: data.totalItems
      });
      setInputPage(data.page); 
    } catch (error) {
      console.error("Erro ao carregar catálogo:", error);
      navigate('/error', { replace: true });
    } finally {
      setLoading(false);
      setIsFirstLoad(false); 
    }
  };

  const handlePageChange = (newPage) => {
    if (newPage >= 1 && newPage <= pagination.totalPages) {
      loadProducts(newPage);
      scrollToCatalog();
    }
  };

  const handleManualPageSubmit = (e) => {
    e.preventDefault();
    const pageNum = parseInt(inputPage);
    if (!isNaN(pageNum)) {
        handlePageChange(pageNum);
    }
  };

  const scrollToCatalog = () => {
    const section = document.getElementById('catalogo');
    if (section) section.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <>
      <div className="relative bg-secondary text-white overflow-hidden transition-all duration-500">
        <div 
            className="absolute inset-0 bg-cover bg-center opacity-20 transform scale-105"
            style={{ 
                backgroundImage: `url('${settings.hero_bg_url || 'https://images.unsplash.com/photo-1562564055-71e051d33c19?q=80&w=2070'}' )` 
            }}
        ></div>
        
        <div className="relative max-w-7xl mx-auto px-4 py-24 sm:px-6 lg:px-8 flex flex-col items-center text-center">
          <motion.div 
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8 }}
          >
            <span className="inline-block py-1 px-3 rounded-full bg-white/10 border border-white/20 text-white text-sm font-semibold mb-6 backdrop-blur-sm">
              {settings.hero_badge}
            </span>
            <h2 className="text-5xl md:text-7xl font-extrabold tracking-tight mb-6 leading-tight drop-shadow-lg">
              {settings.hero_title}
            </h2>
            <p className="text-xl text-gray-200 mb-10 max-w-2xl mx-auto drop-shadow-md">
              {settings.hero_subtitle}
            </p>
            <div className="flex gap-4 justify-center">
              <Button variant="success" className="rounded-full px-8 py-4 text-lg shadow-xl shadow-black/20" onClick={scrollToCatalog}>
                Ver Catálogo
              </Button>
            </div>
          </motion.div>
        </div>
      </div>

      <section id="catalogo" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="flex flex-col lg:flex-row justify-between items-end mb-12 gap-6">
          <div>
            <h2 className="text-3xl font-bold text-gray-900 flex items-center gap-2">
              <Printer className="text-primary" />
              {settings.home_products_title}
            </h2>
            <p className="text-gray-500 mt-2">{settings.home_products_subtitle}</p>
          </div>
          
          <div className="w-full lg:w-auto flex flex-col sm:flex-row gap-4">
            <div className="relative flex-grow sm:w-80">
              <input 
                type="text" 
                placeholder="Buscar produto..." 
                className="w-full pl-12 pr-4 py-3 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary outline-none shadow-sm transition-all"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
              <Search className="absolute left-4 top-3.5 text-gray-400" size={20} />
            </div>

            <div className="relative min-w-[200px]">
                <Filter className="absolute left-4 top-3.5 text-gray-400" size={20} />
                <select 
                    className="w-full pl-12 pr-4 py-3 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary outline-none shadow-sm appearance-none cursor-pointer"
                    value={sortOption}
                    onChange={(e) => setSortOption(e.target.value)}
                >
                    <option value="">Mais Recentes</option>
                    <option value="name-asc">Nome (A-Z)</option>
                    <option value="name-desc">Nome (Z-A)</option>
                    <option value="price-asc">Menor Preço</option>
                    <option value="price-desc">Maior Preço</option>
                </select>
            </div>
          </div>
        </div>

        {loading && isFirstLoad ? (
          <div className="text-center py-20">
            <div className="animate-spin rounded-full h-12 w-12 border-4 border-primary border-t-transparent mx-auto mb-4"></div>
            <p className="text-gray-500">Carregando catálogo...</p>
          </div>
        ) : (
          <div className={`transition-opacity duration-300 ${loading ? 'opacity-50 pointer-events-none' : 'opacity-100'}`}>
            {products.length > 0 ? (
              <>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8">
                  {products.map((prod) => (
                    <ProductCard 
                      key={prod.id} 
                      product={prod}
                      purchaseEnabled={settings.purchase_enabled !== 'false'}
                    />
                  ))}
                </div>

                {pagination.totalPages > 1 && (
                    <div className="flex flex-col sm:flex-row justify-center items-center gap-4 mt-16">
                        <Button 
                            variant="outline" 
                            className="text-gray-600 border-gray-300 hover:bg-gray-50"
                            onClick={() => handlePageChange(pagination.page - 1)}
                            disabled={pagination.page === 1 || loading}
                        >
                            <ChevronLeft size={20} /> Anterior
                        </Button>
                        
                        <form onSubmit={handleManualPageSubmit} className="flex items-center gap-2 bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
                            <span className="text-gray-600 text-sm font-medium">Página</span>
                            <input 
                                type="number" 
                                min="1" 
                                max={pagination.totalPages}
                                value={inputPage}
                                onChange={(e) => setInputPage(e.target.value)}
                                onBlur={() => handlePageChange(parseInt(inputPage) || 1)}
                                className="w-12 text-center border border-gray-300 rounded p-1 text-sm outline-none focus:ring-2 focus:ring-primary font-bold text-gray-800"
                                disabled={loading}
                            />
                            <span className="text-gray-400 text-sm">de {pagination.totalPages}</span>
                        </form>

                        <Button 
                            variant="outline"
                            className="text-gray-600 border-gray-300 hover:bg-gray-50" 
                            onClick={() => handlePageChange(pagination.page + 1)}
                            disabled={pagination.page === pagination.totalPages || loading}
                        >
                            Próximo <ChevronRight size={20} />
                        </Button>
                    </div>
                )}
              </>
            ) : (
              <div className="flex flex-col items-center justify-center py-20 bg-gray-50 rounded-2xl border-2 border-dashed border-gray-200 text-center">
                <p className="text-gray-500 text-lg mb-4">Nenhum produto encontrado com estes filtros.</p>
                <Button variant="ghost" className="text-primary" onClick={() => {setSearchTerm(''); setSortOption('');}}>
                    Limpar Filtros
                </Button>
              </div>
            )}
          </div>
        )}
      </section>
    </>
  );
};