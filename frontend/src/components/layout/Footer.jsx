import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ContentService } from '../../services/contentService';
import { Phone, Mail, MapPin } from 'lucide-react';

export const Footer = () => {
  const [settings, setSettings] = useState(null);
  const [pages, setPages] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadData = async () => {
      try {
        const [settingsData, pagesData] = await Promise.all([
            ContentService.getSettings(),
            ContentService.getAllPages()
        ]);

        if (settingsData) setSettings(settingsData);
        if (pagesData) setPages(pagesData);
      } catch (error) {
        console.error("Erro ao carregar rodapé", error);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  return (
    <footer className="bg-gray-900 text-gray-300 py-12 border-t border-gray-800">
      <div className="max-w-7xl mx-auto px-4 grid md:grid-cols-3 gap-8 text-center md:text-left">
        
        {/* Marca */}
        <div>
          <h3 className="text-white text-lg font-bold mb-4 flex items-center justify-center md:justify-start gap-2">
            {loading ? (
                <span className="text-gray-500 animate-pulse">Carregando...</span>
            ) : (
                <>
                    <span className="w-8 h-8 bg-gray-700 rounded flex items-center justify-center text-white font-bold">X</span>
                    {settings?.site_name || 'Gráfica A Moderna'}
                </>
            )}
          </h3>
          <p className="text-sm leading-relaxed text-gray-400">
            Tecnologia de impressão de ponta gerenciada dinamicamente para oferecer a melhor qualidade do mercado.
          </p>
        </div>

        {/* Links Dinâmicos */}
        <div>
          <h3 className="text-white text-lg font-bold mb-4">Informações</h3>
          {loading ? (
             <div className="space-y-2 animate-pulse">
                <div className="h-4 bg-gray-800 rounded w-1/2 mx-auto md:mx-0"></div>
                <div className="h-4 bg-gray-800 rounded w-2/3 mx-auto md:mx-0"></div>
             </div>
          ) : (
            <ul className="space-y-3 text-sm">
                <li><Link to="/" className="hover:text-blue-400 transition-colors">Início</Link></li>
                {pages.map(page => (
                <li key={page.id}>
                    <Link to={`/pagina/${page.slug}`} className="hover:text-blue-400 transition-colors">
                        {page.title}
                    </Link>
                </li>
                ))}
                <li><Link to="/contato" className="hover:text-blue-400 transition-colors">Contato</Link></li>
            </ul>
          )}
        </div>

        {/* Contato */}
        <div>
          <h3 className="text-white text-lg font-bold mb-4">Contato</h3>
          {loading ? (
             <div className="space-y-3 animate-pulse">
                <div className="h-4 bg-gray-800 rounded w-3/4 mx-auto md:mx-0"></div>
                <div className="h-4 bg-gray-800 rounded w-full mx-auto md:mx-0"></div>
             </div>
          ) : (
            <ul className="space-y-3 text-sm">
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <Phone size={16} className="text-blue-500" />
                    {settings?.whatsapp_display || '(00) 0000-0000'}
                </li>
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <Mail size={16} className="text-blue-500" />
                    {settings?.contact_email || 'email@exemplo.com'}
                </li>
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <MapPin size={16} className="text-blue-500" />
                    {settings?.address || 'Endereço não informado'}
                </li>
            </ul>
          )}
        </div>
      </div>
      
      <div className="text-center mt-12 pt-8 border-t border-gray-800 text-xs text-gray-600">
        © {new Date().getFullYear()} {loading ? '...' : (settings?.site_name || 'Gráfica')}. Todos os direitos reservados.
      </div>
    </footer>
  );
};