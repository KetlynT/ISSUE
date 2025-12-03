import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { CartService } from '../services/cartService';
import { AddressService } from '../services/addressService';
import { ShippingService } from '../services/shippingService';
import { useCart } from '../context/CartContext';
import { Button } from '../components/ui/Button';
import { AddressManager } from '../components/AddressManager';
import { CouponInput } from '../components/CouponInput';
import { MapPin, Truck, CheckCircle, Settings, AlertCircle, Plus } from 'lucide-react';
import toast from 'react-hot-toast';

export const Checkout = () => {
  const [addresses, setAddresses] = useState([]);
  const [selectedAddress, setSelectedAddress] = useState(null);
  const [shippingOptions, setShippingOptions] = useState([]);
  const [selectedShipping, setSelectedShipping] = useState(null);
  
  const [isManageModalOpen, setIsManageModalOpen] = useState(false);
  const [loadingData, setLoadingData] = useState(true);
  const [loadingShipping, setLoadingShipping] = useState(false);
  const [processing, setProcessing] = useState(false);

  const { cartItems, refreshCart } = useCart();
  const navigate = useNavigate();
  const location = useLocation();
  
  const [coupon, setCoupon] = useState(location.state?.coupon || null);

  const loadAddresses = async () => {
    try {
      const data = await AddressService.getAll();
      setAddresses(data);
      if (!selectedAddress) {
          const defaultAddr = data.find(a => a.isDefault) || data[0];
          if (defaultAddr) handleSelectAddress(defaultAddr);
      } else {
          const updated = data.find(a => a.id === selectedAddress.id);
          if (updated) handleSelectAddress(updated);
      }
    } catch (error) {
      toast.error("Erro ao carregar endereços.");
    } finally {
      setLoadingData(false);
    }
  };

  useEffect(() => { loadAddresses(); }, []);

  const handleSelectAddress = async (address) => {
    setSelectedAddress(address);
    setSelectedShipping(null);
    setShippingOptions([]);

    if (!address.zipCode) return;

    setLoadingShipping(true);
    try {
      const itemsForShipping = cartItems.map(i => ({ productId: i.productId, quantity: i.quantity }));
      const options = await ShippingService.calculate(address.zipCode, itemsForShipping);
      setShippingOptions(options);
      if (options.length > 0) setSelectedShipping(options[0]);
    } catch (error) {
      console.error(error);
    } finally {
      setLoadingShipping(false);
    }
  };

  const handleCheckout = async () => {
    if (!selectedAddress || !selectedShipping) return toast.error("Selecione endereço e frete.");
    setProcessing(true);
    try {
      const { id, userId, ...addressPayload } = selectedAddress; 
      
      await CartService.checkout({
        address: addressPayload,
        couponCode: coupon?.code,
        shippingCost: selectedShipping.price,   // CORREÇÃO: Envia o custo
        shippingMethod: selectedShipping.name   // CORREÇÃO: Envia o nome
      });
      
      toast.success("Pedido realizado!");
      await refreshCart();
      navigate('/meus-pedidos');
    } catch (error) {
      toast.error(error.response?.data || "Erro ao processar.");
    } finally {
      setProcessing(false);
    }
  };

  const subTotal = cartItems.reduce((acc, i) => acc + i.totalPrice, 0);
  const discountAmount = coupon ? subTotal * (coupon.discountPercentage / 100) : 0;
  const shippingCost = selectedShipping ? selectedShipping.price : 0;
  const total = subTotal - discountAmount + shippingCost;

  if (loadingData) return <div className="min-h-screen flex items-center justify-center"><div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div></div>;

  return (
    <div className="max-w-6xl mx-auto px-4 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">Finalizar Compra</h1>

      <div className="grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-8">
          
          {/* Endereços */}
          <section className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
            <div className="flex justify-between items-center mb-4">
                <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2"><MapPin className="text-blue-600" /> Endereço de Entrega</h2>
                <Button size="sm" variant="ghost" className="text-blue-600 text-xs gap-1" onClick={() => setIsManageModalOpen(true)}>
                    <Settings size={14}/> Gerenciar Endereços
                </Button>
            </div>

            {addresses.length === 0 ? (
                <div className="text-center py-8 border-2 border-dashed rounded-lg">
                    <p className="text-gray-500 mb-4">Nenhum endereço.</p>
                    <Button onClick={() => setIsManageModalOpen(true)}>Cadastrar Agora</Button>
                </div>
            ) : (
                <div className="grid md:grid-cols-2 gap-4">
                    {addresses.map(addr => (
                        <div key={addr.id} onClick={() => handleSelectAddress(addr)}
                            className={`cursor-pointer p-4 rounded-lg border-2 transition-all relative ${selectedAddress?.id === addr.id ? 'border-blue-600 bg-blue-50 ring-1 ring-blue-600' : 'border-gray-200 hover:border-blue-300 bg-white'}`}>
                            
                            {selectedAddress?.id === addr.id && (
                                <div className="absolute top-2 right-2">
                                    <CheckCircle size={22} className="text-blue-600" fill="currentColor" color="white"/>
                                </div>
                            )}

                            <div className="font-bold text-gray-800 text-sm mb-1">{addr.name}</div>
                            <div className="text-sm text-gray-600 leading-snug">{addr.street}, {addr.number} <br/> {addr.neighborhood} - {addr.city}/{addr.state}</div>
                        </div>
                    ))}
                </div>
            )}
          </section>

          {/* Frete */}
          {selectedAddress && (
              <section className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 animate-in fade-in">
                <h2 className="text-lg font-bold text-gray-800 flex items-center gap-2 mb-4"><Truck className="text-blue-600" /> Envio</h2>
                {loadingShipping ? <div className="text-sm text-gray-500">Calculando...</div> : (
                    <div className="space-y-3">
                        {shippingOptions.map((opt, idx) => (
                            <label key={idx} className={`flex justify-between items-center p-4 rounded-lg border cursor-pointer transition-colors ${selectedShipping?.name === opt.name ? 'border-green-500 bg-green-50' : 'border-gray-200 hover:bg-gray-50'}`}>
                                <div className="flex items-center gap-3">
                                    <input type="radio" name="shipping" checked={selectedShipping?.name === opt.name} onChange={() => setSelectedShipping(opt)} className="text-green-600"/>
                                    <div><div className="font-bold text-gray-800">{opt.name}</div><div className="text-xs text-gray-500">Até {opt.deliveryDays} dias úteis</div></div>
                                </div>
                                <div className="font-bold text-gray-700">R$ {opt.price.toFixed(2)}</div>
                            </label>
                        ))}
                        {shippingOptions.length === 0 && <div className="text-red-500 text-sm flex gap-2"><AlertCircle size={16}/> Falha no cálculo de frete.</div>}
                    </div>
                )}
              </section>
          )}
        </div>

        {/* Resumo */}
        <div className="h-fit bg-white p-6 rounded-xl shadow-lg border border-gray-100 sticky top-24">
            <h3 className="font-bold text-xl text-gray-800 mb-6 border-b pb-4">Resumo</h3>
            
            <div className="mb-6">
                <label className="text-xs font-bold text-gray-500 mb-2 block">CUPOM DE DESCONTO</label>
                <CouponInput onApply={setCoupon} initialCoupon={coupon} />
            </div>

            <div className="space-y-3 text-sm text-gray-600 mb-6">
                <div className="flex justify-between"><span>Subtotal</span><span>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subTotal)}</span></div>
                <div className="flex justify-between text-green-600"><span>Desconto</span><span>- {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(discountAmount)}</span></div>
                <div className="flex justify-between"><span>Frete</span><span>{selectedShipping ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(shippingCost) : '--'}</span></div>
            </div>
            <div className="flex justify-between items-center text-xl font-extrabold text-gray-900 pt-4 border-t border-gray-100 mb-6">
                <span>Total</span><span className="text-blue-700">{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(total)}</span>
            </div>
            <Button onClick={handleCheckout} isLoading={processing} disabled={!selectedAddress || !selectedShipping} variant="success" className="w-full py-4 text-lg">Confirmar Pedido</Button>
        </div>
      </div>

      {/* Modal de Endereços */}
      {isManageModalOpen && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
            <div className="bg-white w-full max-w-2xl rounded-2xl shadow-2xl max-h-[85vh] overflow-hidden flex flex-col">
                <div className="p-5 border-b flex justify-between items-center bg-gray-50">
                    <h3 className="font-bold text-lg text-gray-800">Meus Endereços</h3>
                    <button onClick={() => setIsManageModalOpen(false)}><Plus className="rotate-45 text-gray-400 hover:text-red-500"/></button>
                </div>
                <div className="p-6 overflow-y-auto flex-1">
                    <AddressManager onUpdate={loadAddresses} />
                </div>
            </div>
        </div>
      )}
    </div>
  );
};