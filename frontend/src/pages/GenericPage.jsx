import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ContentService } from '../services/contentService';
import { motion } from 'framer-motion';
import DOMPurify from 'dompurify';

export const GenericPage = () => {
  const { slug } = useParams();
  const [page, setPage] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const loadContent = async () => {
      setLoading(true);
      const data = await ContentService.getPage(slug);
      
      if (!data) {
        // Se a página não existir no banco, redireciona para Home (como pedido)
        navigate('/', { replace: true });
        return;
      }
      
      setPage(data);
      setLoading(false);
    };
    
    loadContent();
  }, [slug, navigate]);

  if (loading) return <div className="min-h-screen flex items-center justify-center"><div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div></div>;

  return (
    <motion.div 
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="max-w-4xl mx-auto px-4 py-16 min-h-screen"
    >
      <h1 className="text-4xl font-bold text-gray-900 mb-8 border-b pb-4">{page.title}</h1>
      
      {/* Renderiza o HTML vindo do banco (Cuidado com XSS em produção real!) */}
      <div 
        className="prose prose-lg prose-blue text-gray-600 max-w-none"
        dangerouslySetInnerHTML={{ __html: page.content }} 
      />
      <div 
        className="prose prose-lg prose-blue text-gray-600 max-w-none"
        dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(page.content) }} 
      />
    </motion.div>
  );
};