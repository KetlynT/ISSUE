'use client'
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { MessageSquare, ShoppingCart, User, LogOut, LayoutDashboard, Package } from 'lucide-react';
import { ContentService } from '../../services/contentService';
import { useCart } from '../../context/CartContext';
import { useAuth } from '../../context/AuthContext'; 

export const Header = () => {
  const [logoUrl, setLogoUrl] = useState('');
  const [siteName, setSiteName] = useState('');
  const [settingsLoading, setSettingsLoading] = useState(true);
  const [purchaseEnabled, setPurchaseEnabled] = useState(true);
  
  const { cartCount } = useCart();
  const { user, logout, isAuthenticated } = useAuth();
  const router = useRouter();
  
  const isAdmin = user?.role === 'Admin';

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await ContentService.getSettings();
        if (settings) {
            if (settings.site_logo) setLogoUrl(settings.site_logo);
            setSiteName(settings.site_name || 'Gráfica Online');
            if (settings.purchase_enabled === 'false') setPurchaseEnabled(false);
        }
      } catch (error) {
        console.error("Erro ao carregar topo", error);
        setSiteName('Gráfica Online');
      } finally {
        setSettingsLoading(false);
      }
    };
    loadSettings();
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/', { replace: true });
  };

  return (
    <header className="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100 transition-all">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
        
        <Link to="/" className="flex items-center gap-2 group">
          {settingsLoading ? (
            <div className="animate-pulse flex items-center gap-2">
                <div className="w-10 h-10 bg-gray-200 rounded-xl"></div>
                <div className="h-4 w-32 bg-gray-200 rounded"></div>
            </div>
          ) : (
            <>
              {logoUrl ? (
                <img src={logoUrl} alt="Logo" className="h-10 w-auto object-contain transition-transform group-hover:scale-105"/>
              ) : (
                <div className="w-10 h-10 bg-gray-900 rounded-xl flex items-center justify-center text-white font-bold text-xl shadow-lg group-hover:shadow-xl transition-all">
                    {siteName.charAt(0) || 'G'}
                </div>
              )}
              <div>
                <h1 className="text-xl font-bold text-gray-800 leading-none tracking-tight">{siteName}</h1>
              </div>
            </>
          )}
        </Link>
        
        <nav className="flex items-center gap-6">
          {settingsLoading ? (
             <div className="animate-pulse h-4 w-24 bg-gray-200 rounded hidden md:block"></div>
          ) : (
             <Link to="/contato" className="hidden md:flex items-center gap-2 text-gray-500 hover:text-primary transition-colors font-medium">
                <MessageSquare size={20} />
                <span className="hidden lg:inline">Fale Conosco</span>
             </Link>
          )}

          {!isAdmin && !settingsLoading && purchaseEnabled && (
            <Link to="/carrinho" className="relative group p-2">
                <ShoppingCart size={24} className="text-gray-600 group-hover:text-primary transition-colors" />
                {cartCount > 0 && (
                    <span className="absolute -top-1 -right-1 bg-red-500 text-white text-[10px] font-bold w-5 h-5 flex items-center justify-center rounded-full shadow-sm animate-bounce">
                        {cartCount}
                    </span>
                )}
            </Link>
          )}

          {!settingsLoading && (
            isAuthenticated ? (
                <div className="flex items-center gap-4 border-l pl-6 border-gray-200">
                    {isAdmin ? (
                        <Link 
                            to="/putiroski/dashboard" 
                            className="flex items-center gap-2 text-sm font-bold text-primary bg-gray-50 px-3 py-2 rounded-lg border border-gray-200 hover:bg-gray-100 transition-all" 
                        >
                            <LayoutDashboard size={18} />
                            <span className="hidden md:inline">Painel</span>
                        </Link>
                    ) : (
                        <>
                            <Link to="/meus-pedidos" className="flex items-center gap-2 text-sm font-medium text-gray-600 hover:text-primary" title="Meus Pedidos">
                                <Package size={20} />
                            </Link>
                            <Link to="/perfil" className="flex items-center gap-2 text-sm font-medium text-gray-600 hover:text-primary" title="Meu Perfil">
                                <User size={20} />
                            </Link>
                        </>
                    )}
                    
                    <button onClick={handleLogout} className="text-gray-400 hover:text-red-500 ml-2" title="Sair">
                        <LogOut size={20} />
                    </button>
                </div>
            ) : (
                purchaseEnabled && (
                    <Link to="/login" className="flex items-center gap-2 text-sm font-bold text-primary hover:brightness-75 border border-gray-200 bg-gray-50 px-4 py-2 rounded-full transition-all hover:shadow-md">
                        <User size={18} /> Entrar
                    </Link>
                )
            )
          )}
        </nav>
      </div>
    </header>
  );
};