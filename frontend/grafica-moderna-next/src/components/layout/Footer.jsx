'use client'
import { useEffect, useState } from 'react';
import Link from 'next/link';
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
    <footer className="bg-footer-bg text-footer-text py-12 border-t border-white/10">
      <div className="max-w-7xl mx-auto px-4 grid md:grid-cols-3 gap-8 text-center md:text-left">
        
        <div>
          <h3 className="text-lg font-bold mb-4 flex items-center justify-center md:justify-start gap-2">
            {loading ? (
                <span className="opacity-50 animate-pulse">Carregando...</span>
            ) : (
                <>
                    <span className="w-8 h-8 bg-primary rounded flex items-center justify-center text-white font-bold">
                        {settings?.site_name?.charAt(0) || 'G'}
                    </span>
                    {settings?.site_name || 'Gráfica A Moderna'}
                </>
            )}
          </h3>
          <p className="text-sm leading-relaxed opacity-80">
            {settings?.footer_about || 'Configure o texto "Sobre" no painel administrativo.'}
          </p>
        </div>

        <div>
          <h3 className="text-lg font-bold mb-4">Informações</h3>
          {loading ? (
             <div className="space-y-2 animate-pulse opacity-50">
                <div className="h-4 bg-gray-500 rounded w-1/2 mx-auto md:mx-0"></div>
                <div className="h-4 bg-gray-500 rounded w-2/3 mx-auto md:mx-0"></div>
             </div>
          ) : (
            <ul className="space-y-3 text-sm">
                <li><Link to="/" className="hover:text-primary transition-colors">Início</Link></li>
                {pages.map(page => (
                <li key={page.id}>
                    <Link to={`/pagina/${page.slug}`} className="hover:text-primary transition-colors">
                        {page.title}
                    </Link>
                </li>
                ))}
                <li><Link to="/contato" className="hover:text-primary transition-colors">Contato</Link></li>
            </ul>
          )}
        </div>

        <div>
          <h3 className="text-lg font-bold mb-4">Contato</h3>
          {loading ? (
             <div className="space-y-3 animate-pulse opacity-50">
                <div className="h-4 bg-gray-500 rounded w-3/4 mx-auto md:mx-0"></div>
             </div>
          ) : (
            <ul className="space-y-3 text-sm">
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <Phone size={16} className="text-primary" />
                    {settings?.whatsapp_display || '(00) 0000-0000'}
                </li>
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <Mail size={16} className="text-primary" />
                    {settings?.contact_email || 'email@exemplo.com'}
                </li>
                <li className="flex items-center justify-center md:justify-start gap-2">
                    <MapPin size={16} className="text-primary" />
                    {settings?.address || 'Endereço não configurado'}
                </li>
            </ul>
          )}
        </div>
      </div>
      
      <div className="text-center mt-12 pt-8 border-t border-white/10 text-xs opacity-60">
        © {new Date().getFullYear()} {settings?.site_name || 'Gráfica'}. Todos os direitos reservados.
      </div>
    </footer>
  );
};