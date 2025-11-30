import React from 'react';
import { Link } from 'react-router-dom';

export const ProductCard = ({ product, onQuote }) => {
  const formattedPrice = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL'
  }).format(product.price);

  return (
    <div className="group bg-white rounded-xl shadow-sm hover:shadow-2xl transition-all duration-300 border border-gray-100 flex flex-col h-full relative overflow-hidden">
      
      {/* Container de Imagem com Zoom e Link para Detalhes */}
      <Link to={`/produto/${product.id}`} className="h-56 w-full bg-gray-100 overflow-hidden relative block cursor-pointer">
         {product.imageUrl ? (
           <img 
             src={product.imageUrl} 
             alt={product.name} 
             className="w-full h-full object-cover transform group-hover:scale-110 transition-transform duration-500" 
           />
         ) : (
           <div className="flex items-center justify-center h-full text-gray-400">
             <span className="text-4xl">📷</span>
           </div>
         )}
         
         {/* Badge de Oferta (Exemplo decorativo) */}
         <div className="absolute top-3 right-3 bg-blue-600 text-white text-xs font-bold px-3 py-1 rounded-full shadow-lg z-10">
            Novo
         </div>
      </Link>

      {/* Conteúdo */}
      <div className="p-5 flex flex-col flex-grow">
        <Link to={`/produto/${product.id}`} className="block">
            <h3 className="text-xl font-bold text-gray-800 mb-2 group-hover:text-blue-600 transition-colors cursor-pointer">
              {product.name}
            </h3>
        </Link>
        
        <p className="text-gray-500 text-sm mb-4 line-clamp-3 flex-grow leading-relaxed">
          {product.description}
        </p>
        
        <div className="mt-auto border-t border-gray-100 pt-4">
          <div className="flex justify-between items-center mb-4">
            <span className="text-xs text-gray-400 uppercase font-semibold">Preço estimado</span>
            <span className="text-2xl font-bold text-blue-600">
              {formattedPrice}
            </span>
          </div>
          
          <button
            onClick={() => onQuote(product)}
            className="w-full bg-green-500 hover:bg-green-600 text-white font-semibold py-3 px-4 rounded-lg transition-colors flex items-center justify-center gap-2 shadow-md hover:shadow-lg"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"/>
            </svg>
            Orçar no WhatsApp
          </button>
        </div>
      </div>
    </div>
  );
};