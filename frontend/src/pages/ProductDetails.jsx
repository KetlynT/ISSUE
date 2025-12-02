import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ProductService } from '../services/productService';
import { ContentService } from '../services/contentService';
import { ShippingService } from '../services/shippingService';
import { useCart } from '../context/CartContext';
import { AuthService } from '../services/authService';
import { Truck, ShoppingCart, MessageSquare, Plus, Minus } from 'lucide-react';
import { Button } from '../components/ui/Button';

export const ProductDetails = () => {
  const { id } = useParams();
  const { addToCart } = useCart();
  
  const [product, setProduct] = useState(null);
  // Estado local para gerenciar a imagem e evitar piscadas
  const [imgSrc, setImgSrc] = useState('');
  
  const [whatsappNumber, setWhatsappNumber] = useState('');
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);

  // Estados Frete
  const [cep, setCep] = useState('');
  const [shippingOptions, setShippingOptions] = useState(null);
  const [calcLoading, setCalcLoading] = useState(false);
  const [calcError, setCalcError] = useState('');

  // Verifica se é admin
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
        setImgSrc(prod.imageUrl); // Inicializa a imagem
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

  const handleCalculateShipping = async (e) => {
    e.preventDefault();
    if (cep.length < 8) {
        setCalcError("Digite um CEP válido.");
        return;
    }
    
    setCalcLoading(true);
    setCalcError('');
    setShippingOptions(null);

    try {
        const options = await ShippingService.calculateForProduct(product.id, cep);
        setShippingOptions(options);
    } catch (error) {
        setCalcError("Erro ao calcular frete. Verifique o CEP.");
    } finally {
        setCalcLoading(false);
    }
  };

  const handleAddToCart = async () => {
    await addToCart(product.id, quantity);
  };

  const handleCustomQuote = () => {
    const message = `Olá! Gostaria de um *orçamento personalizado* para o produto: *${product.name}*. Tenho necessidades específicas...`;
    const number = whatsappNumber || '5511999999999'; 
    window.open(`https://wa.me/${number}?text=${encodeURIComponent(message)}`, '_blank');
  };

  const handleImageError = () => {
    const fallbackUrl = 'https://via.placeholder.com/600x400?text=Sem+Imagem';
    if (imgSrc !== fallbackUrl) {
      setImgSrc(fallbackUrl);
    }
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
                {/* Seletor de Quantidade: Apenas para NÃO Admin */}
                {!isAdmin && (
                    <div className="flex items-center gap-4">
                        <div className="flex items-center border border-gray-300 rounded-lg">
                            <button 
                                onClick={() => setQuantity(q => Math.max(1, q - 1))}
                                className="p-3 text-gray-600 hover:bg-gray-100 transition-colors"
                            >
                                <Minus size={16} />
                            </button>
                            <span className="w-12 text-center font-bold text-gray-800">{quantity}</span>
                            <button 
                                onClick={() => setQuantity(q => q + 1)}
                                className="p-3 text-gray-600 hover:bg-gray-100 transition-colors"
                            >
                                <Plus size={16} />
                            </button>
                        </div>
                        <div className="text-sm text-gray-500">
                            {product.stockQuantity > 0 ? `${product.stockQuantity} disponíveis` : 'Produto Indisponível'}
                        </div>
                    </div>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {/* Botão Adicionar ao Carrinho: Apenas para NÃO Admin */}
                    {!isAdmin && (
                        <Button 
                            onClick={handleAddToCart}
                            disabled={product.stockQuantity < 1}
                            className="w-full py-3 text-base"
                        >
                            <ShoppingCart size={20}/> Adicionar ao Carrinho
                        </Button>
                    )}
                    
                    {/* Botão de Orçamento (Visível para todos) */}
                    <button 
                        onClick={handleCustomQuote}
                        className={`w-full border-2 border-blue-600 text-blue-600 hover:bg-blue-50 font-bold py-3 rounded-lg flex items-center justify-center gap-2 transition-colors ${isAdmin ? 'col-span-2' : ''}`}
                    >
                        <MessageSquare size={20}/> Orçamento Personalizado
                    </button>
                </div>
            </div>

            {/* Calculadora de Frete */}
            <div className="bg-gray-50 p-5 rounded-lg border border-gray-200">
                <h3 className="text-sm font-bold text-gray-700 mb-3 flex items-center gap-2">
                    <Truck size={18} className="text-blue-600"/> Calcular Frete e Prazo
                </h3>
                <form onSubmit={handleCalculateShipping} className="flex gap-2 mb-4">
                    <input 
                        type="text" 
                        placeholder="Digite seu CEP" 
                        maxLength="9"
                        className="flex-1 border border-gray-300 rounded px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-500"
                        value={cep}
                        onChange={(e) => setCep(e.target.value)}
                    />
                    <button 
                        type="submit" 
                        disabled={calcLoading}
                        className="bg-gray-800 hover:bg-gray-900 text-white px-4 py-2 rounded text-sm font-bold transition-colors disabled:opacity-50"
                    >
                        {calcLoading ? '...' : 'OK'}
                    </button>
                </form>

                {calcError && <div className="text-red-500 text-sm mb-2">{calcError}</div>}

                {shippingOptions && (
                    <div className="space-y-2 animate-in fade-in slide-in-from-top-2">
                        {shippingOptions.map((opt, idx) => (
                            <div key={idx} className="flex justify-between items-center bg-white p-3 rounded border border-gray-200 text-sm shadow-sm">
                                <div className="flex flex-col">
                                    <span className="font-bold text-gray-800">{opt.name}</span>
                                    <span className="text-xs text-gray-500">Entrega em até {opt.deliveryDays} dias úteis</span>
                                </div>
                                <span className="font-bold text-green-600">
                                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(opt.price)}
                                </span>
                            </div>
                        ))}
                    </div>
                )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};