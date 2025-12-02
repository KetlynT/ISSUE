import React, { createContext, useState, useEffect, useContext } from 'react';
import { CartService } from '../services/cartService';
import { AuthService } from '../services/authService';
import toast from 'react-hot-toast';

const CartContext = createContext();

export const CartProvider = ({ children }) => {
  const [cartCount, setCartCount] = useState(0);
  const [cartItems, setCartItems] = useState([]); // Armazena itens locais ou remotos
  const [loading, setLoading] = useState(false);

  // Inicialização
  useEffect(() => {
    refreshCart();
  }, []);

  const refreshCart = async () => {
    if (AuthService.isAuthenticated()) {
      // MODO LOGADO: Busca da API
      try {
        const data = await CartService.getCart();
        setCartItems(data.items);
        updateCount(data.items);
      } catch (error) {
        console.error("Erro ao carregar carrinho remoto", error);
      }
    } else {
      // MODO VISITANTE: Busca do LocalStorage
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
    setLoading(true);
    
    // Preparar objeto do item (normalizando dados para UI)
    const newItem = {
      productId: product.id,
      productName: product.name,
      productImage: product.imageUrl,
      unitPrice: product.price,
      quantity: quantity,
      // Dados de frete necessários para cálculo offline
      weight: product.weight,
      width: product.width,
      height: product.height,
      length: product.length,
      totalPrice: product.price * quantity
    };

    try {
      if (AuthService.isAuthenticated()) {
        // API
        await CartService.addItem(newItem.productId, newItem.quantity);
      } else {
        // LOCAL
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
      if(AuthService.isAuthenticated()) toast.success("Adicionado ao carrinho!");
      
    } catch (error) {
      toast.error("Erro ao adicionar produto.");
    } finally {
      setLoading(false);
    }
  };

  const updateQuantity = async (productId, newQuantity) => {
    if (AuthService.isAuthenticated()) {
        // Busca o ID do item no carrinho (a API usa ID do item, não do produto)
        // Precisamos encontrar o ID do CartItem correspondente ao ProductId
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
    if (AuthService.isAuthenticated()) {
        const item = cartItems.find(i => i.productId === productId);
        if(item) await CartService.removeItem(item.id);
    } else {
        const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
        const filtered = localCart.filter(i => i.productId !== productId);
        localStorage.setItem('guest_cart', JSON.stringify(filtered));
    }
    await refreshCart();
  };

  // MÁGICA: Sincroniza o carrinho local com a API após login
  const syncGuestCart = async () => {
    const localCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
    if (localCart.length === 0) {
        await refreshCart();
        return;
    }

    const toastId = toast.loading("Sincronizando carrinho...");
    try {
        // Envia item por item (poderia ser otimizado no backend com um endpoint 'BulkAdd')
        for (const item of localCart) {
            await CartService.addItem(item.productId, item.quantity);
        }
        // Limpa local
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