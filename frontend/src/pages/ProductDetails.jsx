import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ProductService } from '../services/productService';

export const ProductDetails = () => {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadProduct = async () => {
      try {
        const data = await ProductService.getById(id);
        setProduct(data);
      } catch (error) {
        console.error("Erro ao carregar produto", error);
      } finally {
        setLoading(false);
      }
    };
    loadProduct();
  }, [id]);

  if (loading) return <div className="text-center py-20">Carregando...</div>;
  if (!product) return <div className="text-center py-20">Produto não encontrado.</div>;

  const handleQuote = () => {
    const message = `Olá! Vi o produto *${product.name}* no site e gostaria de mais detalhes.`;
    window.open(`https://wa.me/5511999999999?text=${encodeURIComponent(message)}`, '_blank');
  };

  const formattedPrice = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(product.price);

  return (
    <div className="min-h-screen bg-gray-50 py-10 px-4">
      <div className="max-w-4xl mx-auto bg-white rounded-xl shadow-lg overflow-hidden md:flex">
        {/* Imagem Grande */}
        <div className="md:w-1/2 h-96 bg-gray-100">
          <img 
            src={product.imageUrl || 'https://via.placeholder.com/400'} 
            alt={product.name} 
            className="w-full h-full object-cover"
          />
        </div>

        {/* Informações */}
        <div className="md:w-1/2 p-8 flex flex-col justify-between">
          <div>
            <Link to="/" className="text-blue-500 hover:underline text-sm mb-4 block">← Voltar para o catálogo</Link>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">{product.name}</h1>
            <p className="text-gray-500 mb-6 leading-relaxed">{product.description}</p>
            <div className="text-3xl font-bold text-blue-600 mb-6">{formattedPrice}</div>
          </div>

          <button 
            onClick={handleQuote}
            className="w-full bg-green-500 hover:bg-green-600 text-white font-bold py-4 rounded-lg flex items-center justify-center gap-2 transition-colors"
          >
            Orçar no WhatsApp
          </button>
        </div>
      </div>
    </div>
  );
};