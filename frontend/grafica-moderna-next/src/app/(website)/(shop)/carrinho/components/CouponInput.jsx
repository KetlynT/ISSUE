import { useState } from 'react';
import { Tag, X, Loader2 } from 'lucide-react';
import toast from 'react-hot-toast';
import PropTypes from 'prop-types';
import { CouponService } from '@/app/(website)/(shop)/services/couponService';
import { Button } from '@/app/(website)/components/ui/Button';

export const CouponInput = ({ onApply, initialCoupon = null }) => {
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [activeCoupon, setActiveCoupon] = useState(initialCoupon);

  const handleApply = async () => {
    if (!code) return;
    setLoading(true);
    try {
      const data = await CouponService.validate(code);
      setActiveCoupon(data);
      onApply(data);
      toast.success(`Cupom ${data.code} aplicado!`);
      setCode('');
    } catch (error) {
      toast.error("Cupom inválido ou expirado.");
      onApply(null);
    } finally {
      setLoading(false);
    }
  };

  const handleRemove = () => {
    setActiveCoupon(null);
    onApply(null);
    toast.success("Cupom removido.");
  };

  if (activeCoupon) {
    return (
      <div className="bg-green-50 border border-green-200 rounded-lg p-3 flex justify-between items-center animate-in fade-in">
        <div className="flex items-center gap-2 text-green-700">
          <Tag size={18} />
          <span className="font-bold">{activeCoupon.code}</span>
          <span className="text-xs bg-green-200 px-2 py-0.5 rounded-full">-{activeCoupon.discountPercentage}%</span>
        </div>
        <button onClick={handleRemove} className="text-green-600 hover:text-green-800 p-1 hover:bg-green-100 rounded">
          <X size={16} />
        </button>
      </div>
    );
  }

  return (
    <div className="flex gap-2">
      <div className="relative flex-grow">
        <Tag size={18} className="absolute left-3 top-3 text-gray-400" />
        <input 
          className="w-full border border-gray-300 rounded-lg pl-10 p-2.5 outline-none focus:ring-2 focus:ring-primary uppercase transition-all"
          placeholder="CÓDIGO" 
          value={code} 
          onChange={e => setCode(e.target.value)}
          disabled={loading}
        />
      </div>
      <Button onClick={handleApply} disabled={loading || !code} size="sm" className="px-6">
        {loading ? <Loader2 className="animate-spin" size={18}/> : 'Aplicar'}
      </Button>
    </div>
  );
};

CouponInput.propTypes = {
  onApply: PropTypes.func.isRequired,
  initialCoupon: PropTypes.shape({
    code: PropTypes.string,
    discountPercentage: PropTypes.number
  })
};