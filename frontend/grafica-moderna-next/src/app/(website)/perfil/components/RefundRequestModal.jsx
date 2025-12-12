import { useState } from 'react';
import PropTypes from 'prop-types';
import { Button } from '@/app/(website)/components/ui/Button';
import { formatCurrency } from '@/app/(website)/utils/formatters';

export default function RefundRequestModal({ order, onClose, onSubmit, isLoading }) {
  const [refundType, setRefundType] = useState('Total'); 
  const [selectedItems, setSelectedItems] = useState({});

  const handleToggleItem = (productId, maxQuantity) => {
    setSelectedItems(prev => {
      const newState = { ...prev };
      if (newState[productId]) {
        delete newState[productId];
      } else {
        newState[productId] = maxQuantity;
      }
      return newState;
    });
  };

  const handleQuantityChange = (productId, newQuantity, max) => {
    const qty = Math.max(1, Math.min(parseInt(newQuantity) || 1, max));
    setSelectedItems(prev => ({ ...prev, [productId]: qty }));
  };

  const handleSubmit = () => {
    const payload = {
      refundType,
      items: refundType === 'Parcial' 
        ? Object.entries(selectedItems).map(([productId, quantity]) => ({ productId, quantity }))
        : []
    };
    onSubmit(payload);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg p-6 max-w-lg w-full">
        <h2 className="text-xl font-bold mb-4">Solicitar Reembolso</h2>
        
        <div className="mb-4 space-y-2">
          <label className="flex items-center space-x-2">
            <input 
              type="radio" 
              name="type" 
              checked={refundType === 'Total'} 
              onChange={() => setRefundType('Total')}
            />
            <span>Reembolso Total (Pedido Inteiro)</span>
          </label>
          <label className="flex items-center space-x-2">
            <input 
              type="radio" 
              name="type" 
              checked={refundType === 'Parcial'} 
              onChange={() => setRefundType('Parcial')}
            />
            <span>Reembolso Parcial (Selecionar Itens)</span>
          </label>
        </div>

        {refundType === 'Parcial' && (
          <div className="mb-4 max-h-60 overflow-y-auto border p-2 rounded">
            {order.items.map((item, idx) => {
                const key = item.productId || idx; 
                const isSelected = !!selectedItems[key];

                return (
                  <div key={key} className="flex items-center justify-between py-2 border-b last:border-0">
                    <div className="flex items-center gap-2">
                      <input 
                        type="checkbox" 
                        checked={isSelected}
                        onChange={() => handleToggleItem(key, item.quantity)}
                      />
                      <span className="text-sm">{item.productName}</span>
                    </div>
                    {isSelected && (
                      <div className="flex items-center gap-1">
                        <input 
                          type="number" 
                          className="w-16 p-1 border rounded text-right"
                          value={selectedItems[key]}
                          onChange={(e) => handleQuantityChange(key, e.target.value, item.quantity)}
                          min="1"
                          max={item.quantity}
                        />
                        <span className="text-xs text-gray-500">/ {item.quantity}</span>
                      </div>
                    )}
                  </div>
                );
            })}
          </div>
        )}

        <div className="bg-gray-100 p-3 rounded mb-4 text-right">
          <span className="text-gray-600 text-sm">Valor Estimado do Estorno:</span>
          <p className="text-lg font-bold text-green-600">
            {refundType === 'Total' ? formatCurrency(order.totalAmount) : 'R$ A calcular pelo admin'}
          </p>
          <p className="text-xs text-gray-500 mt-1">
            * O valor final será analisado e confirmado pelo administrador.
          </p>
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isLoading}>Cancelar</Button>
          <Button 
            onClick={handleSubmit} 
            isLoading={isLoading}
            disabled={refundType === 'Parcial' && Object.keys(selectedItems).length === 0}
          >
            Confirmar Solicitação
          </Button>
        </div>
      </div>
    </div>
  );
}

RefundRequestModal.propTypes = {
  order: PropTypes.shape({
    totalAmount: PropTypes.number.isRequired,
    items: PropTypes.arrayOf(PropTypes.shape({
      productId: PropTypes.string,
      productName: PropTypes.string,
      quantity: PropTypes.number,
      unitPrice: PropTypes.number
    })).isRequired
  }).isRequired,
  onClose: PropTypes.func.isRequired,
  onSubmit: PropTypes.func.isRequired,
  isLoading: PropTypes.bool
};