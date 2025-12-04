import React, { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { ProductService } from '../services/productService';
import { ContentService } from '../services/contentService';
import { useCart } from '../context/CartContext';
import { AuthService } from '../services/authService';
import { ShoppingCart, MessageSquare, Plus, Minus, Zap } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { ShippingCalculator } from '../components/ShippingCalculator'; // Importado

export const ProductDetails = () => {
  const { id } = useParams();
  const { addToCart } = useCart();
  const navigate = useNavigate();
  
  const [product, setProduct] = useState(null);
  const [imgSrc, setImgSrc] = useState('');
  const [whatsappNumber, setWhatsappNumber] = useState('');
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);

  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const isAdmin = AuthService.isAuthenticated() && user.role === 'Admin';

  useEffect(() => {
    const loadData = async () => {
      try {
        const [prod, settings] = await Promise.all([
             ProductService.getById(id),
             ContentService.getSettings()
        ]);
        setProduct(prod);
        setImgSrc(prod.imageUrl);
        if (settings && settings.whatsapp_number) {
            setWhatsappNumber(settings.whatsapp_number);
        }
      } catch (error) {
        console.error("Erro ao carregar dados", error);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [id]);

  const handleAddToCart = async () => {
    await addToCart(product, quantity);
  };

  const handleBuyNow = async () => {
    await addToCart(product, quantity);
    navigate('/carrinho');
  };

  const handleCustomQuote = () => {
    const message = `Olá! Gostaria de um *orçamento personalizado* para o produto: *${product.name}*. Tenho necessidades específicas...`;
    const number = whatsappNumber || '5511999999999'; 
    window.open(`https://wa.me/${number}?text=${encodeURIComponent(message)}`, '_blank');
  };

  const handleImageError = () => {
    const fallbackUrl = 'https://placehold.co/600x400?text=Sem+Imagem';
    if (imgSrc !== fallbackUrl) setImgSrc(fallbackUrl);
  };

  if (loading) return <div className="text-center py-20">Carregando...</div>;
  if (!product) return <div className="text-center py-20">Produto não encontrado.</div>;

  const formattedPrice = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(product.price);

  return (
    <div className="min-h-screen bg-gray-50 py-10 px-4">
      <div className="max-w-6xl mx-auto bg-white rounded-xl shadow-lg overflow-hidden md:flex">
        
        {/* Coluna Esquerda: Imagem */}
        <div className="md:w-1/2 h-96 md:h-auto bg-gray-100 relative group">
          <img 
            src={imgSrc} 
            alt={product.name} 
            onError={handleImageError}
            className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        </div>

        {/* Coluna Direita: Detalhes e Ações */}
        <div className="md:w-1/2 p-8 flex flex-col">
          <div className="mb-auto">
            <Link to="/" className="text-blue-500 hover:underline text-sm mb-4 block">← Voltar para o catálogo</Link>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">{product.name}</h1>
            <div className="text-3xl font-bold text-blue-600 mb-6">{formattedPrice} <span className="text-sm text-gray-400 font-normal">/ unidade</span></div>
            
            <p className="text-gray-500 mb-8 leading-relaxed whitespace-pre-line border-b pb-6">
                {product.description}
            </p>

            <div className="flex flex-col gap-4 mb-8">
                {/* Seletor de Quantidade */}
                {!isAdmin && (
                    <div className="flex items-center gap-4">
                        <div className="flex items-center border border-gray-300 rounded-lg">
                            <button onClick={() => setQuantity(q => Math.max(1, q - 1))} className="p-3 text-gray-600 hover:bg-gray-100"><Minus size={16} /></button>
                            <span className="w-12 text-center font-bold text-gray-800">{quantity}</span>
                            <button onClick={() => setQuantity(q => q + 1)} className="p-3 text-gray-600 hover:bg-gray-100"><Plus size={16} /></button>
                        </div>
                        <div className="text-sm text-gray-500">
                            {product.stockQuantity > 0 ? `${product.stockQuantity} disponíveis` : 'Indisponível'}
                        </div>
                    </div>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {!isAdmin && (
                        <>
                            <Button onClick={handleAddToCart} disabled={product.stockQuantity < 1} variant="outline" className="text-blue-600 border-blue-600 hover:bg-blue-50">
                                <ShoppingCart size={20}/> Adicionar
                            </Button>
                            
                            <Button onClick={handleBuyNow} disabled={product.stockQuantity < 1} className="w-full py-3 text-base shadow-blue-500/30">
                                <Zap size={20}/> Comprar Agora
                            </Button>
                        </>
                    )}
                    
                    <button 
                        onClick={handleCustomQuote}
                        className={`w-full border-2 border-gray-200 text-gray-600 hover:bg-gray-50 font-bold py-3 rounded-lg flex items-center justify-center gap-2 transition-colors ${isAdmin ? 'col-span-2' : 'col-span-2'}`}
                    >
                        <MessageSquare size={20}/> Orçamento Personalizado
                    </button>
                </div>
            </div>

            {/* Calculadora de Frete Reutilizável */}
            <ShippingCalculator 
                productId={product.id} 
                className="bg-gray-50 border border-gray-200"
            />
          </div>
        </div>
      </div>
    </div>
  );
};