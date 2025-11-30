import React from 'react';
import { Link } from 'react-router-dom';
import { User, MessageSquare } from 'lucide-react'; // Adicionei MessageSquare para o ícone

export const Header = () => {
  return (
    <header className="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-2 group">
          <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-indigo-700 rounded-xl flex items-center justify-center text-white font-bold text-xl shadow-lg group-hover:rotate-12 transition-transform">
            M
          </div>
          <div>
            <h1 className="text-xl font-bold text-gray-800 leading-none">Gráfica</h1>
            <span className="text-xs text-blue-600 font-bold tracking-widest uppercase">A Moderna</span>
          </div>
        </Link>
        
        {/* Navegação */}
        <nav className="flex items-center gap-6">
          {/* Link Fale Conosco NOVO */}
          <Link to="/contato" className="flex items-center gap-2 text-gray-500 hover:text-blue-600 transition-colors font-medium">
            <MessageSquare size={20} />
            <span className="hidden sm:inline">Fale Conosco</span>
          </Link>

          <Link to="/admin" className="flex items-center gap-2 text-gray-500 hover:text-blue-600 transition-colors font-medium">
            <User size={20} />
            <span className="hidden sm:inline">Área Admin</span>
          </Link>
        </nav>
      </div>
    </header>
  );
};