import React, { useEffect, useState } from 'react';
import { ProductService } from '../services/productService';
import { ProductCard } from '../components/ProductCard';

export const Home = () => {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");

  useEffect(() => {
    const fetchCatalog = async () => {
      try {
        const data = await ProductService.getAll();
        setProducts(data);
      } catch (error) {
        console.error("Erro API:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchCatalog();
  }, []);

  const handleQuoteRedirect = (product) => {
    const phoneNumber = "5511999999999"; 
    const message = `Olá! Gostaria de um orçamento para: *${product.name}*. Vi no site por ${new Intl.NumberFormat('pt-BR', {style: 'currency', currency: 'BRL'}).format(product.price)}`;
    const url = `https://wa.me/${phoneNumber}?text=${encodeURIComponent(message)}`;
    window.open(url, '_blank');
  };

  const filteredProducts = products.filter(product =>
    product.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="min-h-screen bg-gray-50 font-sans">
      {/* Navbar */}
      <header className="bg-white shadow-sm sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center text-white font-bold">M</div>
            <h1 className="text-xl font-bold text-gray-800 tracking-tight">
              Gráfica <span className="text-blue-600">A Moderna</span>
            </h1>
          </div>
          <nav>
             <button className="text-gray-500 hover:text-blue-600 font-medium">Login Admin</button>
          </nav>
        </div>
      </header>

      {/* Hero Section */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white py-20 px-4">
        <div className="max-w-7xl mx-auto text-center">
          <h2 className="text-4xl md:text-5xl font-extrabold mb-6 tracking-tight">
            Impressão Profissional para seu Negócio
          </h2>
          <p className="text-lg md:text-xl text-blue-100 mb-8 max-w-2xl mx-auto">
            Cartões de visita, banners, adesivos e muito mais com a qualidade que sua marca merece.
          </p>
          <div className="flex justify-center gap-4">
            <a href="#catalogo" className="bg-green-500 hover:bg-green-600 text-white font-bold py-3 px-8 rounded-full transition-all transform hover:scale-105 shadow-lg border-2 border-transparent">
              Ver Produtos
            </a>
            <button className="bg-transparent border-2 border-white hover:bg-white hover:text-blue-900 text-white font-bold py-3 px-8 rounded-full transition-all">
              Fale Conosco
            </button>
          </div>
        </div>
      </div>

      <main id="catalogo" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        
        {/* Barra de Busca e Título */}
        <div className="flex flex-col md:flex-row justify-between items-center mb-10 gap-4">
          <div>
            <h2 className="text-3xl font-bold text-gray-900">Nossos Produtos</h2>
            <p className="mt-1 text-gray-500">Confira as opções disponíveis para produção imediata.</p>
          </div>
          
          <div className="relative w-full md:w-96">
            <input 
              type="text" 
              placeholder="O que você procura?" 
              className="w-full pl-10 pr-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent shadow-sm"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <svg className="w-5 h-5 text-gray-400 absolute left-3 top-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
          </div>
        </div>

        {/* Grid de Produtos */}
        {loading ? (
          <div className="flex flex-col justify-center items-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-200 border-t-blue-600"></div>
            <p className="mt-4 text-gray-400 font-medium">Carregando catálogo...</p>
          </div>
        ) : filteredProducts.length > 0 ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8">
            {filteredProducts.map((prod) => (
              <ProductCard 
                key={prod.id} 
                product={prod} 
                onQuote={handleQuoteRedirect} 
              />
            ))}
          </div>
        ) : (
          <div className="text-center py-20 bg-white rounded-xl border border-dashed border-gray-300">
            <p className="text-xl text-gray-500">Nenhum produto encontrado para "{searchTerm}"</p>
            <button 
                onClick={() => setSearchTerm("")}
                className="mt-4 text-blue-600 hover:text-blue-800 font-medium underline"
            >
                Limpar busca
            </button>
          </div>
        )}
      </main>
      
      {/* Footer */}
      <footer className="bg-gray-900 text-gray-400 py-8 mt-12">
          <div className="max-w-7xl mx-auto px-4 text-center">
              <p>© {new Date().getFullYear()} Gráfica A Moderna. Todos os direitos reservados.</p>
          </div>
      </footer>
    </div>
  );
};