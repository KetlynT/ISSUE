import { useEffect, useState } from 'react';
import { Button } from '../../components/ui/Button';
import toast from 'react-hot-toast';
import { CouponService } from '../../services/couponService';
import { Trash2 } from 'lucide-react';

const CouponsTab = () => {
    const [coupons, setCoupons] = useState([]);
    const [form, setForm] = useState({ code: '', discountPercentage: '', validityDays: '30' });

    useEffect(() => { load(); }, []);

    const load = async () => {
        try {
            const data = await CouponService.getAll();
            setCoupons(data);
        } catch (e) { console.error(e); }
    };

    const handleCreate = async (e) => {
        e.preventDefault();
        try {
            await CouponService.create({ 
                ...form, 
                discountPercentage: parseFloat(form.discountPercentage),
                validityDays: parseInt(form.validityDays)
            });
            toast.success("Cupom criado!");
            setForm({ code: '', discountPercentage: '', validityDays: '30' });
            load();
        } catch (e) {
            toast.error(e.response?.data || "Erro ao criar");
        }
    };

    const handleDelete = async (id) => {
        if(!confirm("Excluir cupom?")) return;
        await CouponService.delete(id);
        load();
    };

    return (
        <div className="grid md:grid-cols-3 gap-8">
            <div className="bg-white p-6 rounded-xl shadow border border-gray-100 h-fit">
                <h3 className="font-bold text-gray-800 mb-4">Novo Cupom</h3>
                <form onSubmit={handleCreate} className="space-y-4">
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">CÓDIGO</label>
                        <input className="w-full border p-2 rounded uppercase" value={form.code} onChange={e => setForm({...form, code: e.target.value.toUpperCase()})} required placeholder="Ex: PROMO10"/>
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">Desconto (%)</label>
                        <input type="number" className="w-full border p-2 rounded" value={form.discountPercentage} onChange={e => setForm({...form, discountPercentage: e.target.value})} required placeholder="10"/>
                    </div>
                    <div>
                        <label className="block text-xs font-bold text-gray-500 mb-1">Validade (Dias)</label>
                        <input type="number" className="w-full border p-2 rounded" value={form.validityDays} onChange={e => setForm({...form, validityDays: e.target.value})} required/>
                    </div>
                    <Button type="submit" className="w-full">Criar Cupom</Button>
                </form>
            </div>

            <div className="md:col-span-2 bg-white rounded-xl shadow border border-gray-100 overflow-hidden">
                <table className="w-full text-left">
                    <thead className="bg-gray-50 border-b text-gray-600 text-sm uppercase">
                        <tr>
                            <th className="p-4">Código</th>
                            <th className="p-4">Desconto</th>
                            <th className="p-4">Expira em</th>
                            <th className="p-4 text-right">Ação</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                        {coupons.map(c => (
                            <tr key={c.id}>
                                <td className="p-4 font-bold text-gray-800">{c.code}</td>
                                <td className="p-4 text-green-600 font-bold">{c.discountPercentage}%</td>
                                <td className="p-4 text-sm text-gray-500">{new Date(c.expiryDate).toLocaleDateString('pt-BR')}</td>
                                <td className="p-4 text-right">
                                    <button onClick={() => handleDelete(c.id)} className="text-red-500 hover:bg-red-50 p-2 rounded"><Trash2 size={18}/></button>
                                </td>
                            </tr>
                        ))}
                        {coupons.length === 0 && <tr><td colSpan="4" className="p-8 text-center text-gray-400">Nenhum cupom ativo.</td></tr>}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

export default CouponsTab;