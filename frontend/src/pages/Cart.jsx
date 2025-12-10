import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { Button } from '../components/ui/Button';
import { CouponInput } from '../components/CouponInput';
import { ShippingCalculator } from '../components/ShippingCalculator';
import { Trash2, ShoppingBag, ArrowRight, Plus, Minus } from 'lucide-react';
import toast from 'react-hot-toast';
import AuthService from '../services/authService';

export const Cart = () => {
  const { cartItems, updateQuantity, removeFromCart } = useCart();
  const navigate = useNavigate();

  const [appliedCoupon, setAppliedCoupon] = useState(null);
  const [selectedShipping, setSelectedShipping] = useState(null);

  const handleUpdateQuantity = async (productId, currentQty, delta) => {
    const newQty = currentQty + delta;
    if (newQty < 1) return;
    await updateQuantity(productId, newQty);
    
    if (selectedShipping) {
        setSelectedShipping(null);
        toast('Quantidade alterada. Por favor, calcule o frete novamente.', { icon: '🚚' });
    }
  };

  const handleCheckout = () => {
    if (!AuthService.isAuthenticated()) {
        toast("Faça login para finalizar a compra.");
        navigate('/login', { state: { from: '/carrinho' } });
        return;
    }
    navigate('/checkout', { state: { coupon: appliedCoupon } });
  };

  const subTotal = cartItems.reduce((acc, i) => acc + (i.totalPrice || 0), 0);
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
                {cartItems.map((item, idx) => (
                    <tr key={item.productId || idx}>
                    <td className="p-4">
                        <div className="flex items-center gap-4">
                        {item.productImage ? (
                            <img src={item.productImage} className="w-16 h-16 object-cover rounded border" alt="" />
                        ) : (
                            <div className="w-16 h-16 bg-gray-200 rounded border flex items-center justify-center text-xs">Sem Imagem</div>
                        )}
                        <div>
                            <div className="font-bold text-gray-800">{item.productName || 'Item Indisponível'}</div>
                            <div className="text-xs text-gray-500">Unit: R$ {(item.unitPrice || 0).toFixed(2)}</div>
                        </div>
                        </div>
                    </td>
                    <td className="p-4">
                        <div className="flex items-center justify-center border rounded-lg w-fit mx-auto">
                            <button onClick={() => handleUpdateQuantity(item.productId, item.quantity, -1)} className="px-3 py-1 text-gray-600 hover:bg-gray-100 disabled:opacity-50" disabled={item.quantity <= 1}><Minus size={14} /></button>
                            <span className="px-2 font-medium text-sm w-8 text-center">{item.quantity}</span>
                            <button onClick={() => handleUpdateQuantity(item.productId, item.quantity, 1)} className="px-3 py-1 text-gray-600 hover:bg-gray-100"><Plus size={14} /></button>
                        </div>
                    </td>
                    <td className="p-4 text-right font-bold text-primary">R$ {(item.totalPrice || 0).toFixed(2)}</td>
                    <td className="p-4 text-right">
                        <button onClick={() => removeFromCart(item.productId)} className="text-red-500 hover:bg-red-50 p-2 rounded-full"><Trash2 size={18} /></button>
                    </td>
                    </tr>
                ))}
                </tbody>
            </table>
            </div>

            <ShippingCalculator 
                items={cartItems} 
                onSelectOption={setSelectedShipping} 
                className="bg-white shadow-sm border border-gray-100"
            />
        </div>

        <div className="h-fit space-y-6">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2">Cupom de Desconto</h3>
            <CouponInput onApply={setAppliedCoupon} initialCoupon={appliedCoupon} />
          </div>

          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <h3 className="font-bold text-lg text-gray-800 mb-4 border-b pb-2">Resumo</h3>
            <div className="space-y-2 text-sm text-gray-600 mb-4">
                <div className="flex justify-between">
                    <span>Subtotal</span>
                    <span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subTotal)}</span>
                </div>
                
                {appliedCoupon && (
                    <div className="flex justify-between text-green-600 font-medium">
                        <span>Desconto ({appliedCoupon.code})</span>
                        <span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(discountAmount)}</span>
                    </div>
                )}

                <div className="flex justify-between text-primary">
                    <span>Frete</span>
                    <span>{selectedShipping ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(shippingCost) : 'Não calculado'}</span>
                </div>
            </div>

            <div className="flex justify-between items-center text-lg font-bold text-gray-900 mt-4 pt-4 border-t border-gray-100 mb-6">
              <span>Total</span>
              <span className="text-2xl text-primary">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(total)}
              </span>
            </div>
            
            <Button onClick={handleCheckout} className="w-full py-4 text-lg" variant="success">
                {AuthService.isAuthenticated() ? 'Continuar para Entrega' : 'Login para Finalizar'} <ArrowRight size={20} />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};