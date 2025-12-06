import React, { createContext, useState, useEffect, useContext } from 'react';
import { CartService } from '../services/cartService';
import AuthService from '../services/authService'; 
import toast from 'react-hot-toast';
import { useAuth } from './AuthContext'; // 1. Importar o useAuth

const CartContext = createContext();

export const CartProvider = ({ children }) => {
  const { user } = useAuth(); // 2. Pegar o usuário atual (que contém a role)
  const [cartCount, setCartCount] = useState(0);
  const [cartItems, setCartItems] = useState([]); 
  const [loading, setLoading] = useState(false);

  // 3. Reage a mudanças no usuário (Login, Logout ou carregamento inicial)
  useEffect(() => {
    refreshCart();
  }, [user]); 

  const refreshCart = async () => {
    // 4. BLOQUEIO PARA ADMIN: Se for admin, limpa estado e não faz requisição
    if (user && user.role === 'Admin') {
        setCartItems([]);
        setCartCount(0);
        return; 
    }

    // Lógica para Usuário Logado (Role: User)
    // Usamos 'user' do contexto em vez de AuthService.isAuthenticated() para garantir consistência
    if (user) { 
      try {
        const data = await CartService.getCart();
        setCartItems(data.items);
        updateCount(data.items);
      } catch (error) {
        // Se der erro (ex: 403 ou 401), não crasha, apenas loga
        console.error("Erro ao carregar carrinho remoto", error);
      }
    } else {
      // Lógica para Visitante (Guest)
      const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
      setCartItems(localCart);
      updateCount(localCart);
    }
  };

  const updateCount = (items) => {
    const count = items.reduce((acc, item) => acc + item.quantity, 0);
    setCartCount(count);
  };

  const addToCart = async (product, quantity = 1) => {
    // Segurança extra: Admin não adiciona itens
    if (user && user.role === 'Admin') {
        toast.error("Administradores não podem fazer compras.");
        return;
    }

    setLoading(true);
    
    const newItem = {
      productId: product.id,
      productName: product.name,
      productImage: product.imageUrl,
      unitPrice: product.price,
      quantity: quantity,
      weight: product.weight,
      width: product.width,
      height: product.height,
      length: product.length,
      totalPrice: product.price * quantity
    };

    try {
      if (user) { // Usuário logado
        await CartService.addItem(newItem.productId, newItem.quantity);
      } else { // Guest
        const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
        const existingIndex = localCart.findIndex(i => i.productId === newItem.productId);

        if (existingIndex >= 0) {
          localCart[existingIndex].quantity += newItem.quantity;
          localCart[existingIndex].totalPrice = localCart[existingIndex].quantity * localCart[existingIndex].unitPrice;
        } else {
          localCart.push(newItem);
        }

        localStorage.setItem('guest_cart', JSON.stringify(localCart));
        toast.success("Adicionado ao carrinho temporário");
      }
      
      await refreshCart();
      if(user) toast.success("Adicionado ao carrinho!");
      
    } catch (error) {
      toast.error("Erro ao adicionar produto.");
    } finally {
      setLoading(false);
    }
  };

  const updateQuantity = async (productId, newQuantity) => {
    if (user && user.role === 'Admin') return; // Bloqueio

    if (user) {
        const item = cartItems.find(i => i.productId === productId);
        if(item) await CartService.updateQuantity(item.id, newQuantity);
    } else {
        const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
        const updated = localCart.map(item => {
            if (item.productId === productId) {
                return { ...item, quantity: newQuantity, totalPrice: item.unitPrice * newQuantity };
            }
            return item;
        });
        localStorage.setItem('guest_cart', JSON.stringify(updated));
    }
    await refreshCart();
    return true;
  };

  const removeFromCart = async (productId) => {
    if (user && user.role === 'Admin') return;

    if (user) {
        const item = cartItems.find(i => i.productId === productId);
        if(item) await CartService.removeItem(item.id);
    } else {
        const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
        const filtered = localCart.filter(i => i.productId !== productId);
        localStorage.setItem('guest_cart', JSON.stringify(filtered));
    }
    await refreshCart();
  };

  const syncGuestCart = async () => {
    // Admin nunca sincroniza carrinho
    if (user && user.role === 'Admin') {
        localStorage.removeItem('guest_cart'); // Limpa lixo se houver
        return;
    }

    const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
    if (localCart.length === 0) {
        await refreshCart();
        return;
    }

    const toastId = toast.loading("Sincronizando carrinho...");
    try {
        for (const item of localCart) {
            await CartService.addItem(item.productId, item.quantity);
        }
        localStorage.removeItem('guest_cart');
        await refreshCart();
        toast.success("Carrinho sincronizado!", { id: toastId });
    } catch (e) {
        toast.error("Erro ao sincronizar alguns itens.", { id: toastId });
    }
  };

  return (
    <CartContext.Provider value={{ 
        cartCount, 
        cartItems, 
        addToCart, 
        updateQuantity, 
        removeFromCart, 
        refreshCart,
        syncGuestCart, 
        loading 
    }}>
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);