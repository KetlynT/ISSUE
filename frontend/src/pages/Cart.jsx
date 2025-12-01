import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { CartService } from '../services/cartService';
import { CouponService } from '../services/couponService';
import { ShippingService } from '../services/shippingService'; // Importar Shipping
import { Button } from '../components/ui/Button';
import { Trash2, ShoppingBag, ArrowRight, Tag, Truck, Plus, Minus } from 'lucide-react';
import toast from 'react-hot-toast';

export const Cart = () => {
  const { cartCount, fetchCartCount, updateQuantity } = useCart();
  const navigate = useNavigate();

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Lógica de Cupom
  const [couponCode, setCouponCode] = useState('');
  const [appliedCoupon, setAppliedCoupon] = useState(null);
  const [couponLoading, setCouponLoading] = useState(false);

  // Lógica de Frete
  const [cep, setCep] = useState('');
  const [shippingOptions, setShippingOptions] = useState(null);
  const [selectedShipping, setSelectedShipping] = useState(null); // Opção de frete escolhida
  const [shippingLoading, setShippingLoading] = useState(false);

  useEffect(() => {
    loadCart();
  }, [cartCount]); // Recarrega se o contador global mudar

  const loadCart = async () => {
    try {
        const data = await CartService.getCart();
        setItems(data.items);
    } catch (e) {
        console.error(e);
    } finally {
        setLoading(false);
    }
  };

  const handleUpdateQuantity = async (itemId, newQty) => {
    if (newQty < 1) return;
    const success = await updateQuantity(itemId, newQty);
    if(success) {
        // Atualiza localmente para feedback instantâneo
        setItems(prev => prev.map(i => i.id === itemId ? {...i, quantity: newQty, totalPrice: i.unitPrice * newQty} : i));
        // Se mudou quantidade, o frete calculado anteriormente pode estar errado, ideal resetar ou recalcular
        if (shippingOptions) handleCalculateShipping(); 
    }
  };

  const handleRemove = async (itemId) => {
    await CartService.removeItem(itemId);
    setItems(prev => prev.filter(i => i.id !== itemId));
    fetchCartCount();
    if(items.length <= 1) {
        setShippingOptions(null);
        setSelectedShipping(null);
    }
  };

  const handleApplyCoupon = async () => {
    if(!couponCode) return;
    setCouponLoading(true);
    try {
      const data = await CouponService.validate(couponCode);
      setAppliedCoupon(data);
      toast.success(`Cupom ${data.code} aplicado! ${data.discountPercentage}% OFF`);
    } catch (error) {
      setAppliedCoupon(null);
      toast.error("Cupom inválido ou expirado.");
    } finally {
      setCouponLoading(false);
    }
  };

  const handleCalculateShipping = async (e) => {
    if(e) e.preventDefault();
    if (cep.length < 8) {
        toast.error("CEP inválido");
        return;
    }
    
    setShippingLoading(true);
    try {
        // Prepara os itens para a API de frete
        const shippingItems = items.map(i => ({
            weight: i.weight,
            width: i.width,
            height: i.height,
            length: i.length,
            quantity: i.quantity
        }));

        const options = await ShippingService.calculate(cep, shippingItems);
        setShippingOptions(options);
        // Seleciona a primeira opção automaticamente se não houver selecionada
        if(options.length > 0 && !selectedShipping) setSelectedShipping(options[0]);
    } catch (error) {
        toast.error("Erro ao calcular frete");
    } finally {
        setShippingLoading(false);
    }
  };

  const handleCheckout = () => {
    if (!selectedShipping && items.length > 0) {
        toast.error("Por favor, calcule e selecione o frete antes de continuar.");
        return;
    }

    // Passamos o cupom E o frete via state
    // Nota: O backend atual calcula o frete novamente ou salva? 
    // Como o backend atual não salva o frete no banco (Order), vamos considerar 
    // que o valor total será ajustado visualmente, mas num cenário real o backend deveria receber o valor do frete.
    navigate('/checkout', { state: { coupon: appliedCoupon } });
  };

  // Cálculos de Totais
  const subTotal = items.reduce((acc, i) => acc + i.totalPrice, 0);
  const discountAmount = appliedCoupon ? subTotal * (appliedCoupon.discountPercentage / 100) : 0;
  const shippingCost = selectedShipping ? selectedShipping.price : 0;
  
  const total = subTotal - discountAmount + shippingCost;

  if (loading) return <div className="text-center py-20">Carregando carrinho...</div>;

  if (items.length === 0) {
    return (
      <div className="min-h-[60vh] flex flex-col items-center justify-center text-center px-4">
        <div className="bg-gray-100 p-6 rounded-full mb-4">
            <ShoppingBag size={48} className="text-gray-400" />
        </div>
        <h2 className="text-2xl font-bold text-gray-800 mb-2">Seu carrinho está vazio</h2>
        <Link to="/"><Button>Voltar para a Loja</Button></Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-8 flex items-center gap-2">
        <ShoppingBag /> Meu Carrinho
      </h1>

      <div className="grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden h-fit">
            <table className="w-full text-left">
                <thead className="bg-gray-50 text-gray-600 text-sm uppercase">
                <tr>
                    <th className="p-4">Produto</th>
                    <th className="p-4 text-center">Qtd</th>
                    <th className="p-4 text-right">Total</th>
                    <th className="p-4"></th>
                </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                {items.map((item) => (
                    <tr key={item.id}>
                    <td className="p-4">
                        <div className="flex items-center gap-4">
                        {item.productImage && <img src={item.productImage} className="w-16 h-16 object-cover rounded border" />}
                        <div>
                            <div className="font-bold text-gray-800">{item.productName}</div>
                            <div className="text-xs text-gray-500">Unit: R$ {item.unitPrice.toFixed(2)}</div>
                        </div>
                        </div>
                    </td>
                    <td className="p-4">
                        <div className="flex items-center justify-center border rounded-lg w-fit mx-auto">
                            <button 
                                onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                                className="px-3 py-1 text-gray-600 hover:bg-gray-100 disabled:opacity-50"
                                disabled={item.quantity <= 1}
                            >
                                <Minus size={14} />
                            </button>
                            <span className="px-2 font-medium text-sm w-8 text-center">{item.quantity}</span>
                            <button 
                                onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                                className="px-3 py-1 text-gray-600 hover:bg-gray-100"
                            >
                                <Plus size={14} />
                            </button>
                        </div>
                    </td>
                    <td className="p-4 text-right font-bold text-blue-600">R$ {item.totalPrice.toFixed(2)}</td>
                    <td className="p-4 text-right">
                        <button onClick={() => handleRemove(item.id)} className="text-red-500 hover:bg-red-50 p-2 rounded-full"><Trash2 size={18} /></button>
                    </td>
                    </tr>
                ))}
                </tbody>
            </table>
            </div>

            {/* Calculadora de Frete no Carrinho */}
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2"><Truck size={18}/> Calcular Frete</h3>
                <form onSubmit={handleCalculateShipping} className="flex gap-2 max-w-sm mb-4">
                    <input 
                        className="flex-1 border border-gray-300 rounded-lg px-3 py-2 outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="CEP de Entrega"
                        value={cep}
                        onChange={e => setCep(e.target.value)}
                        maxLength={9}
                    />
                    <Button type="submit" isLoading={shippingLoading} size="sm" className="bg-gray-800 hover:bg-gray-900">Calcular</Button>
                </form>

                {shippingOptions && (
                    <div className="space-y-2 mt-4">
                        {shippingOptions.map((opt, idx) => (
                            <label key={idx} className={`flex justify-between items-center p-3 rounded border cursor-pointer transition-colors ${selectedShipping?.name === opt.name ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-blue-300'}`}>
                                <div className="flex items-center gap-3">
                                    <input 
                                        type="radio" 
                                        name="shipping" 
                                        checked={selectedShipping?.name === opt.name}
                                        onChange={() => setSelectedShipping(opt)}
                                        className="text-blue-600"
                                    />
                                    <div>
                                        <div className="font-bold text-sm text-gray-800">{opt.name}</div>
                                        <div className="text-xs text-gray-500">até {opt.deliveryDays} dias úteis</div>
                                    </div>
                                </div>
                                <div className="font-bold text-gray-700">R$ {opt.price.toFixed(2)}</div>
                            </label>
                        ))}
                    </div>
                )}
            </div>
        </div>

        <div className="h-fit space-y-6">
          {/* Cupom */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2"><Tag size={18}/> Cupom de Desconto</h3>
            <div className="flex gap-2">
                <input 
                    className="flex-1 border border-gray-300 rounded-lg px-3 py-2 outline-none focus:ring-2 focus:ring-blue-500 uppercase"
                    placeholder="CÓDIGO"
                    value={couponCode}
                    onChange={e => setCouponCode(e.target.value)}
                />
                <Button onClick={handleApplyCoupon} isLoading={couponLoading} size="sm">Aplicar</Button>
            </div>
            {appliedCoupon && (
                <div className="mt-2 text-sm text-green-600 bg-green-50 p-2 rounded border border-green-200 text-center">
                    Cupom <b>{appliedCoupon.code}</b> aplicado: -{appliedCoupon.discountPercentage}%
                </div>
            )}
          </div>

          {/* Resumo */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-lg text-gray-800 mb-4 border-b pb-2">Resumo do Pedido</h3>
            
            <div className="space-y-2 text-sm text-gray-600 mb-4">
                <div className="flex justify-between">
                    <span>Subtotal</span>
                    <span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subTotal)}</span>
                </div>
                
                {selectedShipping && (
                    <div className="flex justify-between">
                        <span>Frete</span>
                        <span>+ {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(shippingCost)}</span>
                    </div>
                )}

                {appliedCoupon && (
                    <div className="flex justify-between text-green-600 font-medium">
                        <span>Desconto</span>
                        <span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(discountAmount)}</span>
                    </div>
                )}
            </div>

            <div className="flex justify-between items-center text-lg font-bold text-gray-900 mt-4 pt-4 border-t border-gray-100 mb-6">
              <span>Total</span>
              <span className="text-2xl text-blue-600">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(total)}
              </span>
            </div>
            
            <Button onClick={handleCheckout} className="w-full py-4 text-lg" variant="success">
              Finalizar Compra <ArrowRight size={20} />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};