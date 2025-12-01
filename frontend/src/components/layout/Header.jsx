// ... (imports anteriores, certifique-se de importar User)
import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { MessageSquare, ShoppingCart, User, LogOut, Package } from 'lucide-react';
import { ContentService } from '../../services/contentService';
import { useCart } from '../../context/CartContext';
import { AuthService } from '../../services/authService';

export const Header = () => {
  const [logoUrl, setLogoUrl] = useState('');
  const { cartCount } = useCart();
  const navigate = useNavigate();
  const isAuthenticated = AuthService.isAuthenticated();

  useEffect(() => {
    ContentService.getSettings().then(settings => {
      if (settings && settings.site_logo) setLogoUrl(settings.site_logo);
    });
  }, []);

  const handleLogout = () => {
    AuthService.logout();
    navigate('/login');
  };

  return (
    <header className="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
        <Link to="/" className="flex items-center gap-2 group">
          {logoUrl ? (
            <img src={logoUrl} alt="Logo" className="h-10 w-auto object-contain transition-transform group-hover:scale-105"/>
          ) : (
            <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-indigo-700 rounded-xl flex items-center justify-center text-white font-bold text-xl shadow-lg">M</div>
          )}
          <div>
            <h1 className="text-xl font-bold text-gray-800 leading-none">Gráfica</h1>
            <span className="text-xs text-blue-600 font-bold tracking-widest uppercase">A Moderna</span>
          </div>
        </Link>
        
        <nav className="flex items-center gap-6">
          <Link to="/contato" className="hidden md:flex items-center gap-2 text-gray-500 hover:text-blue-600 transition-colors font-medium">
            <MessageSquare size={20} />
            <span className="hidden lg:inline">Fale Conosco</span>
          </Link>

          <Link to="/carrinho" className="relative group p-2">
            <ShoppingCart size={24} className="text-gray-600 group-hover:text-blue-600 transition-colors" />
            {cartCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-red-500 text-white text-[10px] font-bold w-5 h-5 flex items-center justify-center rounded-full shadow-sm animate-bounce">
                    {cartCount}
                </span>
            )}
          </Link>

          {isAuthenticated ? (
            <div className="flex items-center gap-4 border-l pl-6 border-gray-200">
                <Link to="/meus-pedidos" className="flex items-center gap-2 text-sm font-medium text-gray-600 hover:text-blue-600" title="Meus Pedidos">
                    <Package size={20} />
                </Link>
                <Link to="/perfil" className="flex items-center gap-2 text-sm font-medium text-gray-600 hover:text-blue-600" title="Meu Perfil">
                    <User size={20} />
                </Link>
                <button onClick={handleLogout} className="text-gray-400 hover:text-red-500" title="Sair">
                    <LogOut size={20} />
                </button>
            </div>
          ) : (
            <Link to="/portal-acesso-secreto-gm" className="flex items-center gap-2 text-sm font-bold text-blue-600 hover:text-blue-800 border border-blue-100 bg-blue-50 px-4 py-2 rounded-full transition-all hover:shadow-md">
                <User size={18} /> Entrar
            </Link>
          )}
        </nav>
      </div>
    </header>
  );
};