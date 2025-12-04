import React, { useState } from 'react';
import { ShippingService } from '../services/shippingService';
import { Button } from './ui/Button';
import { Truck, AlertCircle, Loader2 } from 'lucide-react';
import toast from 'react-hot-toast';

export const ShippingCalculator = ({ items = [], productId = null, onSelectOption, className }) => {
  const [cep, setCep] = useState('');
  const [options, setOptions] = useState(null);
  const [selectedOption, setSelectedOption] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleCalculate = async (e) => {
    e.preventDefault();
    if (cep.length < 8) {
      setError("Digite um CEP válido.");
      return;
    }

    setLoading(true);
    setError('');
    setOptions(null);
    setSelectedOption(null);
    if(onSelectOption) onSelectOption(null);

    try {
      let result = [];
      
      // Se tiver productId, calcula para 1 item. Senão, usa a lista de items do carrinho.
      if (productId) {
        result = await ShippingService.calculateForProduct(productId, cep);
      } else if (items.length > 0) {
        // Formata os itens para o formato que a API espera
        const payloadItems = items.map(i => ({
            productId: i.productId,
            quantity: i.quantity
        }));
        result = await ShippingService.calculate(cep, payloadItems);
      } else {
        setError("Nenhum item para calcular.");
        setLoading(false);
        return;
      }

      setOptions(result);
    } catch (err) {
      console.error(err);
      setError("Erro ao calcular frete. Verifique o CEP.");
      toast.error("Não foi possível calcular o frete.");
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (option) => {
    setSelectedOption(option);
    if (onSelectOption) {
      onSelectOption(option);
    }
  };

  return (
    <div className={`bg-gray-50 p-5 rounded-lg border border-gray-200 ${className}`}>
      <h3 className="text-sm font-bold text-gray-700 mb-3 flex items-center gap-2">
        <Truck size={18} className="text-blue-600"/> Calcular Frete e Prazo
      </h3>
      
      <form onSubmit={handleCalculate} className="flex gap-2 mb-4">
        <input 
          type="text" 
          placeholder="Digite seu CEP" 
          maxLength="9"
          className="flex-1 border border-gray-300 rounded px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          value={cep} 
          onChange={(e) => setCep(e.target.value.replace(/\D/g, '').replace(/^(\d{5})(\d)/, '$1-$2'))}
        />
        <Button type="submit" disabled={loading} size="sm" className="bg-gray-800 hover:bg-gray-900">
          {loading ? <Loader2 className="animate-spin" size={16}/> : 'OK'}
        </Button>
      </form>

      {error && (
        <div className="text-red-500 text-sm mb-3 flex items-center gap-1">
          <AlertCircle size={14}/> {error}
        </div>
      )}

      {options && (
        <div className="space-y-2 animate-in fade-in slide-in-from-top-2">
          {options.length === 0 && <div className="text-sm text-gray-500">Nenhuma opção disponível.</div>}
          
          {options.map((opt, idx) => (
            <div 
              key={idx} 
              onClick={() => handleSelect(opt)}
              className={`flex justify-between items-center p-3 rounded border cursor-pointer transition-all ${
                selectedOption?.name === opt.name 
                  ? 'bg-blue-50 border-blue-500 ring-1 ring-blue-500' 
                  : 'bg-white border-gray-200 hover:border-blue-300'
              }`}
            >
              <div className="flex items-center gap-3">
                <div className={`w-4 h-4 rounded-full border flex items-center justify-center ${selectedOption?.name === opt.name ? 'border-blue-600' : 'border-gray-300'}`}>
                    {selectedOption?.name === opt.name && <div className="w-2 h-2 bg-blue-600 rounded-full"/>}
                </div>
                <div>
                  <span className="font-bold text-gray-800 text-sm block">{opt.name}</span>
                  <span className="text-xs text-gray-500">Até {opt.deliveryDays} dias úteis</span>
                </div>
              </div>
              <span className="font-bold text-green-700 text-sm">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(opt.price)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};