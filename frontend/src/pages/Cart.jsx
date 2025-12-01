import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { CartService } from '../services/cartService';
import { CouponService } from '../services/couponService';
import { Button } from '../components/ui/Button';
import { Trash2, ShoppingBag, ArrowRight, Tag } from 'lucide-react';
import toast from 'react-hot-toast';

export const Cart = () => {
  const { cartCount, fetchCartCount } = useCart();
  const navigate = useNavigate();

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Lógica de Cupom
  const [couponCode, setCouponCode] = useState('');
  const [appliedCoupon, setAppliedCoupon] = useState(null); // Objeto: { code, discountPercentage }
  const [couponLoading, setCouponLoading] = useState(false);

  useEffect(() => {
    CartService.getCart().then(data => {
      setItems(data.items);
      setLoading(false);
    }).catch(() => setLoading(false));
  }, [cartCount]);

  const handleRemove = async (itemId) => {
    await CartService.removeItem(itemId);
    setItems(prev => prev.filter(i => i.id !== itemId));
    fetchCartCount();
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

  const handleCheckout = () => {
    // Passamos o cupom via state para a página de checkout usar
    navigate('/checkout', { state: { coupon: appliedCoupon } });
  };

  // Cálculos de Totais
  const subTotal = items.reduce((acc, i) => acc + i.totalPrice, 0);
  const discountAmount = appliedCoupon ? subTotal * (appliedCoupon.discountPercentage / 100) : 0;
  const total = subTotal - discountAmount;

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
        <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden h-fit">
          <table className="w-full text-left">
            <thead className="bg-gray-50 text-gray-600 text-sm uppercase">
              <tr>
                <th className="p-4">Produto</th>
                <th className="p-4 text-center">Qtd</th>
                <th className="p-4 text-right">Preço</th>
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
                  <td className="p-4 text-center font-medium">{item.quantity}</td>
                  <td className="p-4 text-right font-bold text-blue-600">R$ {item.totalPrice.toFixed(2)}</td>
                  <td className="p-4 text-right">
                    <button onClick={() => handleRemove(item.id)} className="text-red-500 hover:bg-red-50 p-2 rounded-full"><Trash2 size={18} /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
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
                <div className="mt-2 text-sm text-green-600 bg-green-50 p-2 rounded border border-green-200">
                    Cupom <b>{appliedCoupon.code}</b> aplicado: -{appliedCoupon.discountPercentage}%
                </div>
            )}
          </div>

          {/* Resumo */}
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-lg text-gray-800 mb-4 border-b pb-2">Resumo</h3>
            
            <div className="flex justify-between text-gray-600 mb-2">
                <span>Subtotal</span>
                <span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subTotal)}</span>
            </div>

            {appliedCoupon && (
                <div className="flex justify-between text-green-600 mb-2 font-medium">
                    <span>Desconto</span>
                    <span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(discountAmount)}</span>
                </div>
            )}

            <div className="flex justify-between items-center text-lg font-bold text-gray-900 mt-4 pt-4 border-t border-gray-100 mb-6">
              <span>Total</span>
              <span className="text-2xl text-blue-600">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(total)}
              </span>
            </div>
            
            <Button onClick={handleCheckout} className="w-full py-4 text-lg" variant="success">
              Continuar Compra <ArrowRight size={20} />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};