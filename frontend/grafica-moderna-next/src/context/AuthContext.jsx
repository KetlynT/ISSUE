'use client';

import { createContext, useState, useEffect, useContext } from 'react';
import authService from '../services/authService';
import PropTypes from 'prop-types';
import { useRouter } from 'next/navigation';

const AuthContext = createContext({});

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const checkLoginStatus = async () => {
      try {
        const status = await authService.checkAuth();
        
        if (status.isAuthenticated) {
          const userProfile = await authService.getProfile();
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

  const login = async (credentials, isAdmin = false) => {
    const data = await authService.login(credentials, isAdmin);
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
      router.push('/');
      router.refresh();
    } catch (e) {
      console.error(e);
    } finally {
      setUser(null);
    }
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout, loading, isAuthenticated: !!user }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};

AuthProvider.propTypes = {
  children: PropTypes.node.isRequired
};

export const useAuth = () => useContext(AuthContext);
export default AuthContext;