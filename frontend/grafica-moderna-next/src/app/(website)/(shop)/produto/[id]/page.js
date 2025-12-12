import { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, Plus, Minus, Zap } from 'lucide-react';
import { ProductService } from '@/app/(website)/(shop)/services/productService';
import { ContentService } from '@/app/(website)/services/contentService';
import { useCart } from '@/app/(website)/context/CartContext';
import AuthService from '@/app/(website)/login/services/authService';
import { Button } from '@/app/(website)/components/ui/Button';
import { ShippingCalculator } from '@/app/(website)/(shop)/carrinho/components/ShippingCalculator';

export const ProductDetails = () => {
  const { id } = useParams();
  const { addToCart } = useCart();
  const navigate = useNavigate();
  
  const [product, setProduct] = useState(null);
  const [imgSrc, setImgSrc] = useState('');
  const [whatsappNumber, setWhatsappNumber] = useState('');
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);
  const [purchaseEnabled, setPurchaseEnabled] = useState(true);

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
        if (settings) {
            if (settings.whatsapp_number) {
                setWhatsappNumber(settings.whatsapp_number);
            }
            if (settings.purchase_enabled === 'false') {
                setPurchaseEnabled(false);
            }
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
        
        <div className="md:w-1/2 h-96 md:h-auto bg-gray-100 relative group">
          <img 
            src={imgSrc} 
            alt={product.name} 
            onError={handleImageError}
            className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        </div>

        <div className="md:w-1/2 p-8 flex flex-col">
          <div className="mb-auto">
            <Link to="/" className="text-primary hover:underline text-sm mb-4 block">← Voltar para o catálogo</Link>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">{product.name}</h1>
            <div className="text-3xl font-bold text-primary mb-6">{formattedPrice} <span className="text-sm text-gray-400 font-normal">/ unidade</span></div>
            
            <p className="text-gray-500 mb-8 leading-relaxed whitespace-pre-line border-b pb-6">
                {product.description}
            </p>

            <div className="flex flex-col gap-4 mb-8">
                {!isAdmin && purchaseEnabled && (
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

                {!purchaseEnabled && (
                    <div className="bg-blue-50 text-blue-800 p-4 rounded-lg text-center mb-4 border border-blue-200">
                        <strong>Modo Orçamento:</strong> As compras diretas estão temporariamente pausadas. Utilize o botão abaixo para solicitar um orçamento personalizado.
                    </div>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {!isAdmin && purchaseEnabled && (
                        <>
                            <Button onClick={handleAddToCart} disabled={product.stockQuantity < 1} variant="outline" className="text-primary border-primary hover:bg-primary/5">
                                <ShoppingCart size={20}/> Adicionar
                            </Button>
                            
                            <Button onClick={handleBuyNow} disabled={product.stockQuantity < 1} className="w-full py-3 text-base shadow-primary/30">
                                <Zap size={20}/> Comprar Agora
                            </Button>
                        </>
                    )}
                    
<button
    onClick={handleCustomQuote}
    className={[
        "w-full border-2 font-bold py-3 rounded-lg flex items-center justify-center gap-2 transition-colors",
        "bg-[#25D366] hover:bg-[#1EBE5A]",
        purchaseEnabled && !isAdmin ? "sm:col-span-2" : ""
    ].join(" ")}
>
<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 175.216 175.552">
  <defs>
    <linearGradient id="b" x1="85.915" x2="86.535" y1="32.567" y2="137.092" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#57d163"/>
      <stop offset="1" stop-color="#23b33a"/>
    </linearGradient>

    <filter id="a" width="1.115" height="1.114" x="-.057" y="-.057" color-interpolation-filters="sRGB">
      <feGaussianBlur stdDeviation="3.531"/>
    </filter>
  </defs>

  <path 
    fill="#b3b3b3" 
    d="m54.532 138.45 2.235 1.324c9.387 5.571 20.15 8.518 31.126 8.523h.023c33.707 0 61.139-27.426 61.153-61.135.006-16.335-6.349-31.696-17.895-43.251A60.75 60.75 0 0 0 87.94 25.983c-33.733 0-61.166 27.423-61.178 61.13a60.98 60.98 0 0 0 9.349 32.535l1.455 2.312-6.179 22.558zm-40.811 23.544L24.16 123.88c-6.438-11.154-9.825-23.808-9.821-36.772.017-40.556 33.021-73.55 73.578-73.55 19.681.01 38.154 7.669 52.047 21.572s21.537 32.383 21.53 52.037c-.018 40.553-33.027 73.553-73.578 73.553h-.032c-12.313-.005-24.412-3.094-35.159-8.954z"
    filter="url(#a)"
  />

  <path
    fill="#fff"
    d="m12.966 161.238 10.439-38.114a73.42 73.42 0 0 1-9.821-36.772c.017-40.556 33.021-73.55 73.578-73.55 19.681.01 38.154 7.669 52.047 21.572s21.537 32.383 21.53 52.037c-.018 40.553-33.027 73.553-73.578 73.553h-.032c-12.313-.005-24.412-3.094-35.159-8.954z"
  />

  <path
    fill="url(#b)"
    d="M87.184 25.227c-33.733 0-61.166 27.423-61.178 61.13a60.98 60.98 0 0 0 9.349 32.535l1.455 2.312-6.179 22.559 23.146-6.069 2.235 1.324c9.387 5.571 20.15 8.518 31.126 8.524h.023c33.707 0 61.14-27.426 61.153-61.135a60.75 60.75 0 0 0-17.895-43.251 60.75 60.75 0 0 0-43.235-17.929z"
  />

  <path
    fill="#fff"
    fill-rule="evenodd"
    d="M68.772 55.603c-1.378-3.061-2.828-3.123-4.137-3.176l-3.524-.043c-1.226 0-3.218.46-4.902 2.3s-6.435 6.287-6.435 15.332 6.588 17.785 7.506 19.013 12.718 20.381 31.405 27.75c15.529 6.124 18.689 4.906 22.061 4.6s10.877-4.447 12.408-8.74 1.532-7.971 1.073-8.74-1.685-1.226-3.525-2.146-10.877-5.367-12.562-5.981-2.91-.919-4.137.921-4.746 5.979-5.819 7.206-2.144 1.381-3.984.462-7.76-2.861-14.784-9.124c-5.465-4.873-9.154-10.891-10.228-12.73s-.114-2.835.808-3.751c.825-.824 1.838-2.147 2.759-3.22s1.224-1.84 1.836-3.065.307-2.301-.153-3.22-4.032-10.011-5.666-13.647"
  />
</svg>

    Orçamento Personalizado
</button>

                </div>
            </div>

            {purchaseEnabled && (
                <ShippingCalculator 
                    productId={product.id} 
                    className="bg-gray-50 border border-gray-200"
                />
            )}
          </div>
        </div>
      </div>
    </div>
  );
};