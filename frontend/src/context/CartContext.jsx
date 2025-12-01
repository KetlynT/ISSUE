import React, { createContext, useState, useEffect, useContext } from 'react';
import { CartService } from '../services/cartService';
import { AuthService } from '../services/authService';
import toast from 'react-hot-toast';

const CartContext = createContext();

export const CartProvider = ({ children }) => {
  const [cartCount, setCartCount] = useState(0);
  const [cartTotal, setCartTotal] = useState(0);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (AuthService.isAuthenticated()) {
      fetchCartCount();
    }
  }, []);

  const fetchCartCount = async () => {
    try {
      const cart = await CartService.getCart();
      const count = cart.items.reduce((acc, item) => acc + item.quantity, 0);
      setCartCount(count);
      setCartTotal(cart.grandTotal);
    } catch (error) {
      console.error("Erro ao carregar carrinho", error);
    }
  };

  const addToCart = async (productId, quantity = 1) => {
    if (!AuthService.isAuthenticated()) {
      toast.error("Faça login para adicionar ao carrinho");
      return;
    }

    setLoading(true);
    try {
      await CartService.addItem(productId, quantity);
      toast.success("Produto adicionado!");
      await fetchCartCount();
    } catch (error) {
      toast.error(error.response?.data || "Erro ao adicionar produto");
    } finally {
      setLoading(false);
    }
  };

  // NOVA FUNÇÃO
  const updateQuantity = async (itemId, newQuantity) => {
    try {
        await CartService.updateQuantity(itemId, newQuantity);
        await fetchCartCount(); // Atualiza contadores globais
        return true;
    } catch (error) {
        toast.error(error.response?.data || "Erro ao atualizar quantidade");
        return false;
    }
  };

  return (
    <CartContext.Provider value={{ cartCount, cartTotal, addToCart, updateQuantity, fetchCartCount, loading }}>
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);