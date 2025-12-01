import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { CartService } from '../services/cartService';
import { Button } from '../components/ui/Button';
import { MapPin } from 'lucide-react';
import toast from 'react-hot-toast';
import api from '../services/api'; 

export const Checkout = () => {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    zipCode: '',
    address: '',
    city: '',
    state: ''
  });
  
  const navigate = useNavigate();
  const location = useLocation();
  const coupon = location.state?.coupon; // Recebe o cupom aplicado no Carrinho

  // Carrega dados do perfil para pré-preencher
  useEffect(() => {
    const loadProfile = async () => {
      try {
        const { data } = await api.get('/auth/profile');
        setFormData({
            zipCode: data.zipCode || '',
            address: data.address || '',
            city: data.city || '',
            state: data.state || ''
        });
      } catch (e) {
        console.error("Erro ao carregar perfil", e);
        // Não exibe erro crítico, apenas deixa o form vazio
      }
    };
    loadProfile();
  }, []);

  const handleCheckout = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      // 1. Tenta atualizar o perfil com o endereço usado (Opcional)
      // Recupera o email do localStorage para enviar junto (workaround simples)
      const user = JSON.parse(localStorage.getItem('user'));
      await api.put('/auth/profile', { 
        fullName: user?.fullName || user?.email || 'Cliente', 
        ...formData 
      }).catch(() => {}); // Ignora erro se falhar atualização de perfil

      // 2. Finaliza Compra
      await CartService.checkout({ 
        address: `${formData.address}, ${formData.city}-${formData.state}`, 
        zipCode: formData.zipCode,
        couponCode: coupon?.code // Envia o código do cupom se existir
      });
      
      toast.success("Pedido realizado com sucesso!");
      
      // 3. Redireciona para histórico
      navigate('/meus-pedidos');
      
      // Recarrega a página para garantir que o estado do carrinho no header zere visualmente
      setTimeout(() => window.location.reload(), 50);
    } catch (error) {
        console.error(error);
        toast.error(error.response?.data || "Erro ao finalizar pedido. Tente novamente.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-3xl mx-auto px-4 py-12">
      <div className="text-center mb-10">
        <h1 className="text-3xl font-bold text-gray-900">Finalizar Compra</h1>
        <p className="text-gray-500">Confirme seus dados de entrega</p>
      </div>

      <div className="bg-white p-8 rounded-xl shadow-lg border border-blue-100">
        {coupon && (
            <div className="mb-6 bg-green-50 text-green-700 p-4 rounded-lg border border-green-200 text-sm font-medium text-center">
                🎉 Cupom <b>{coupon.code}</b> aplicado! Você ganhou {coupon.discountPercentage}% de desconto.
            </div>
        )}

        <form onSubmit={handleCheckout} className="space-y-6">
          <div className="flex items-center gap-2 text-blue-600 font-bold border-b pb-2 mb-4">
            <MapPin /> Endereço de Entrega
          </div>

          <div className="grid md:grid-cols-2 gap-4">
            <div>
                <label className="block text-sm font-bold text-gray-700 mb-1">CEP</label>
                <input 
                    required
                    className="w-full border p-3 rounded-lg bg-gray-50 focus:ring-2 focus:ring-blue-500 outline-none"
                    value={formData.zipCode}
                    onChange={e => setFormData({...formData, zipCode: e.target.value})}
                    placeholder="00000-000"
                />
            </div>
            <div>
                <label className="block text-sm font-bold text-gray-700 mb-1">Cidade</label>
                <input 
                    required
                    className="w-full border p-3 rounded-lg bg-gray-50 focus:ring-2 focus:ring-blue-500 outline-none"
                    value={formData.city}
                    onChange={e => setFormData({...formData, city: e.target.value})}
                />
            </div>
            <div className="md:col-span-2">
                <label className="block text-sm font-bold text-gray-700 mb-1">Endereço Completo</label>
                <input 
                    required
                    className="w-full border p-3 rounded-lg bg-gray-50 focus:ring-2 focus:ring-blue-500 outline-none"
                    value={formData.address}
                    onChange={e => setFormData({...formData, address: e.target.value})}
                    placeholder="Rua, Número, Bairro, Complemento"
                />
            </div>
            <div>
                <label className="block text-sm font-bold text-gray-700 mb-1">Estado (UF)</label>
                <input 
                    required
                    maxLength="2"
                    className="w-full border p-3 rounded-lg bg-gray-50 focus:ring-2 focus:ring-blue-500 outline-none uppercase"
                    value={formData.state}
                    onChange={e => setFormData({...formData, state: e.target.value})}
                />
            </div>
          </div>

          <div className="pt-6">
            <Button type="submit" isLoading={loading} variant="success" className="w-full py-4 text-lg shadow-green-500/20">
               Confirmar Pedido
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};