'use client'

import { createContext, useContext, useState, useEffect } from 'react';
import { CartService } from '../services/cartService';
import { useAuth } from './AuthContext';
import toast from 'react-hot-toast';

const CartContext = createContext({});

export const CartProvider = ({ children }) => {
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(false);
  const { user } = useAuth();

  const fetchCart = async () => {
    if (!user || user.role === 'Admin') {
        setCart({ items: [], totalAmount: 0 });
        return;
    }
    
    try {
      setLoading(true);
      const data = await CartService.getCart();
      setCart(data);
    } catch (error) {
      console.error('Erro ao buscar carrinho', error);
      setCart({ items: [], totalAmount: 0 });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCart();
  }, [user]);

  const addToCart = async (product, quantity) => {
    if (!user) {
        toast.error("Você precisa fazer login para adicionar itens ao carrinho.");
        return;
    }

    try {
      await CartService.addItem(product.id, quantity);
      toast.success('Produto adicionado ao carrinho!');
      await fetchCart();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Erro ao adicionar produto');
    }
  };

  const updateQuantity = async (itemId, quantity) => {
    try {
      await CartService.updateQuantity(itemId, quantity);
      await fetchCart();
    } catch (error) {
      toast.error('Erro ao atualizar quantidade');
    }
  };

  const removeItem = async (itemId) => {
    try {
      await CartService.removeItem(itemId);
      toast.success('Item removido!');
      await fetchCart();
    } catch (error) {
      toast.error('Erro ao remover item');
    }
  };

  const clearCart = async () => {
    try {
      await CartService.clearCart();
      setCart({ items: [], totalAmount: 0 });
    } catch (error) {
      toast.error('Erro ao limpar carrinho');
    }
  };

  return (
    <CartContext.Provider value={{ 
        cart, 
        loading, 
        addToCart, 
        updateQuantity, 
        removeItem, 
        clearCart,
        refreshCart: fetchCart 
    }}>
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);
export default CartContext;