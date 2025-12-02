import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext'; // Usamos o estado global agora
import { CouponService } from '../services/couponService';
import { ShippingService } from '../services/shippingService';
import { Button } from '../components/ui/Button';
import { Trash2, ShoppingBag, ArrowRight, Tag, Truck, Plus, Minus } from 'lucide-react';
import toast from 'react-hot-toast';
import { AuthService } from '../services/authService';

export const Cart = () => {
  // Pega itens e funções do Contexto, não mais load manual
  const { cartItems, updateQuantity, removeFromCart } = useCart();
  const navigate = useNavigate();

  // Estados locais apenas para features da página
  const [couponCode, setCouponCode] = useState('');
  const [appliedCoupon, setAppliedCoupon] = useState(null);
  const [couponLoading, setCouponLoading] = useState(false);

  const [cep, setCep] = useState('');
  const [shippingOptions, setShippingOptions] = useState(null);
  const [selectedShipping, setSelectedShipping] = useState(null);
  const [shippingLoading, setShippingLoading] = useState(false);

  const handleUpdateQuantity = async (productId, currentQty, delta) => {
    const newQty = currentQty + delta;
    if (newQty < 1) return;
    await updateQuantity(productId, newQty);
    // Limpa frete pois peso mudou
    if (shippingOptions) {
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
      toast.success(`Cupom aplicado: ${data.discountPercentage}% OFF`);
    } catch (error) {
      setAppliedCoupon(null);
      toast.error("Cupom inválido.");
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
        const shippingItems = cartItems.map(i => ({
            weight: i.weight,
            width: i.width,
            height: i.height,
            length: i.length,
            quantity: i.quantity
        }));

        const options = await ShippingService.calculate(cep, shippingItems);
        setShippingOptions(options);
        if(options.length > 0) setSelectedShipping(options[0]);
    } catch (error) {
        toast.error("Erro ao calcular frete");
    } finally {
        setShippingLoading(false);
    }
  };

  const handleCheckout = () => {
    if (!AuthService.isAuthenticated()) {
        // Redireciona para Login com intenção de voltar
        toast("Faça login para finalizar a compra.");
        navigate('/login');
        return;
    }

    if (!selectedShipping && cartItems.length > 0) {
        toast.error("Selecione o frete antes de continuar.");
        return;
    }

    navigate('/checkout', { state: { coupon: appliedCoupon } });
  };

  // Cálculos
  const subTotal = cartItems.reduce((acc, i) => acc + i.totalPrice, 0);
  const discountAmount = appliedCoupon ? subTotal * (appliedCoupon.discountPercentage / 100) : 0;
  const shippingCost = selectedShipping ? selectedShipping.price : 0;
  const total = subTotal - discountAmount + shippingCost;

  if (cartItems.length === 0) {
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
                {cartItems.map((item) => (
                    <tr key={item.productId}>
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
                                onClick={() => handleUpdateQuantity(item.productId, item.quantity, -1)}
                                className="px-3 py-1 text-gray-600 hover:bg-gray-100 disabled:opacity-50"
                                disabled={item.quantity <= 1}
                            >
                                <Minus size={14} />
                            </button>
                            <span className="px-2 font-medium text-sm w-8 text-center">{item.quantity}</span>
                            <button 
                                onClick={() => handleUpdateQuantity(item.productId, item.quantity, 1)}
                                className="px-3 py-1 text-gray-600 hover:bg-gray-100"
                            >
                                <Plus size={14} />
                            </button>
                        </div>
                    </td>
                    <td className="p-4 text-right font-bold text-blue-600">R$ {item.totalPrice.toFixed(2)}</td>
                    <td className="p-4 text-right">
                        <button onClick={() => removeFromCart(item.productId)} className="text-red-500 hover:bg-red-50 p-2 rounded-full"><Trash2 size={18} /></button>
                    </td>
                    </tr>
                ))}
                </tbody>
            </table>
            </div>

            {/* Frete */}
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2"><Truck size={18}/> Calcular Frete</h3>
                <form onSubmit={handleCalculateShipping} className="flex gap-2 max-w-sm mb-4">
                    <input 
                        className="flex-1 border border-gray-300 rounded-lg px-3 py-2 outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="CEP de Entrega" value={cep} onChange={e => setCep(e.target.value)} maxLength={9}
                    />
                    <Button type="submit" isLoading={shippingLoading} size="sm" className="bg-gray-800 hover:bg-gray-900">Calcular</Button>
                </form>

                {shippingOptions && (
                    <div className="space-y-2 mt-4">
                        {shippingOptions.map((opt, idx) => (
                            <label key={idx} className={`flex justify-between items-center p-3 rounded border cursor-pointer transition-colors ${selectedShipping?.name === opt.name ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-blue-300'}`}>
                                <div className="flex items-center gap-3">
                                    <input type="radio" name="shipping" checked={selectedShipping?.name === opt.name} onChange={() => setSelectedShipping(opt)} className="text-blue-600"/>
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
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2"><Tag size={18}/> Cupom</h3>
            <div className="flex gap-2">
                <input className="flex-1 border border-gray-300 rounded-lg px-3 py-2 outline-none focus:ring-2 focus:ring-blue-500 uppercase" placeholder="CÓDIGO" value={couponCode} onChange={e => setCouponCode(e.target.value)}/>
                <Button onClick={handleApplyCoupon} isLoading={couponLoading} size="sm">Aplicar</Button>
            </div>
            {appliedCoupon && <div className="mt-2 text-sm text-green-600 bg-green-50 p-2 rounded border border-green-200 text-center">Cupom <b>{appliedCoupon.code}</b> aplicado!</div>}
          </div>

          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-lg text-gray-800 mb-4 border-b pb-2">Resumo</h3>
            <div className="space-y-2 text-sm text-gray-600 mb-4">
                <div className="flex justify-between"><span>Subtotal</span><span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subTotal)}</span></div>
                {selectedShipping && <div className="flex justify-between"><span>Frete</span><span>+ {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(shippingCost)}</span></div>}
                {appliedCoupon && <div className="flex justify-between text-green-600 font-medium"><span>Desconto</span><span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(discountAmount)}</span></div>}
            </div>
            <div className="flex justify-between items-center text-lg font-bold text-gray-900 mt-4 pt-4 border-t border-gray-100 mb-6">
              <span>Total</span>
              <span className="text-2xl text-blue-600">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(total)}</span>
            </div>
            <Button onClick={handleCheckout} className="w-full py-4 text-lg" variant="success">
                {AuthService.isAuthenticated() ? 'Finalizar Compra' : 'Login para Finalizar'} <ArrowRight size={20} />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};