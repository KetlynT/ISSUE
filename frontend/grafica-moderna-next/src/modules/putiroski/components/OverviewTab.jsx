import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { RefreshCcw, DollarSign, ShoppingBag, AlertTriangle } from 'lucide-react';
import { DashboardService } from '../../services/dashboardService';

const OverviewTab = () => {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        DashboardService.getStats()
            .then(setStats)
            .catch(() => toast.error("Erro ao carregar estatísticas"))
            .finally(() => setLoading(false));
    }, []);

    if (loading) return <div className="text-center py-10">Carregando painel...</div>;
    if (!stats) return null;

    return (
        <div className="space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Receita Líquida</div>
                        <div className="p-2 bg-green-100 text-green-600 rounded-lg"><DollarSign size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">
                        {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(stats.totalRevenue)}
                    </div>
                </div>

                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Total Reembolsado</div>
                        <div className="p-2 bg-purple-100 text-purple-600 rounded-lg"><RefreshCcw size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">
                        {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(stats.totalRefunded || 0)}
                    </div>
                </div>
                
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Pedidos Totais</div>
                        <div className="p-2 bg-blue-100 text-blue-600 rounded-lg"><ShoppingBag size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">{stats.totalOrders}</div>
                    <div className="text-xs text-gray-500 mt-1">{stats.pendingOrders} pendentes de envio</div>
                </div>

                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <div className="flex items-center justify-between mb-4">
                        <div className="text-gray-500 text-xs font-bold uppercase">Alerta de Estoque</div>
                        <div className="p-2 bg-red-100 text-red-600 rounded-lg"><AlertTriangle size={20}/></div>
                    </div>
                    <div className="text-2xl font-bold text-gray-800">{stats.lowStockProducts.length}</div>
                    <div className="text-xs text-gray-500 mt-1">Itens críticos</div>
                </div>
            </div>
        </div>
    );
};

export default OverviewTab;