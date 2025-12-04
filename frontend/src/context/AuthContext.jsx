import React, { createContext, useState, useEffect, useContext } from 'react';
import authService from '../services/authService';

const AuthContext = createContext({});

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null); // Objeto do utilizador
  const [loading, setLoading] = useState(true); // Para não mostrar a tela antes de checar auth

  // Ao iniciar a App, pergunta ao backend se estamos logados
  useEffect(() => {
    const checkLoginStatus = async () => {
      try {
        const status = await authService.checkAuth();
        if (status.isAuthenticated) {
          // Se o cookie for válido, buscamos os detalhes do perfil
          const userProfile = await authService.getProfile();
          // Combinamos a role do checkAuth com os dados do perfil
          setUser({ ...userProfile, role: status.role });
        } else {
          setUser(null);
        }
      } catch (error) {
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    checkLoginStatus();
  }, []);

  const login = async (credentials) => {
    const data = await authService.login(credentials);
    // Após login sucesso, buscamos o perfil completo para garantir estado atualizado
    // O backend já retornou role e email, mas podemos querer o UserID ou Nome
    const userProfile = await authService.getProfile(); 
    setUser({ ...userProfile, role: data.role });
    return data;
  };

  const register = async (userData) => {
    const data = await authService.register(userData);
    const userProfile = await authService.getProfile();
    setUser({ ...userProfile, role: data.role });
    return data;
  };

  const logout = async () => {
    try {
      await authService.logout();
    } finally {
      setUser(null);
      // Opcional: window.location.href = '/login'; para forçar refresh total
    }
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout, loading, isAuthenticated: !!user }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};

// Hook personalizado para facilitar o uso
export const useAuth = () => useContext(AuthContext);

export default AuthContext;