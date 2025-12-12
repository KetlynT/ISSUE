import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import DOMPurify from 'dompurify';
import { ContentService } from '@/app/(website)/services/contentService';

export const GenericPage = () => {
  const { slug } = useParams();
  const [page, setPage] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const loadContent = async () => {
      setLoading(true);
      try {
        const data = await ContentService.getPage(slug);
        
        if (!data) {
          navigate('/', { replace: true });
          return;
        }
        
        setPage(data);
      } catch (error) {
        console.error("Erro ao carregar página:", error);
        navigate('/', { replace: true });
      } finally {
        setLoading(false);
      }
    };
    
    loadContent();
  }, [slug, navigate]);

  if (loading) return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div>
    </div>
  );

  if (!page) return null;

  const sanitizedContent = DOMPurify.sanitize(page.content, {
    USE_PROFILES: { html: true },
    ADD_ATTR: ['target']
  });

  return (
    <motion.div 
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="max-w-4xl mx-auto px-4 py-16 min-h-screen"
    >
      <h1 className="text-4xl font-bold text-gray-900 mb-8 border-b pb-4">{page.title}</h1>
      
      <div 
        className="prose prose-lg prose-blue text-gray-600 max-w-none"
        dangerouslySetInnerHTML={{ __html: sanitizedContent }} 
      />
    </motion.div>
  );
};