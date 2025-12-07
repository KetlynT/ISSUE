import React, { createContext, useState, useEffect, useContext } from 'react';
import authService from '../services/authService';
import api from '../services/api';

const AuthContext = createContext({});

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkLoginStatus = async () => {
      try {
        const token = localStorage.getItem('access_token');
        if (!token) {
            setUser(null);
            setLoading(false);
            return;
        }
        const status = await authService.checkAuth();
        if (status.isAuthenticated) {
          const userProfile = await authService.getProfile();
          setUser({ ...userProfile, role: status.role });
        } else {
          localStorage.removeItem('access_token');
          setUser(null);
        }
      } catch (error) {
        localStorage.removeItem('access_token');
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    checkLoginStatus();
  }, []);

  const login = async (credentials, isAdmin = false) => {
    const data = await authService.login(credentials, isAdmin);
    if (data.token) {
        localStorage.setItem('access_token', data.token);
    }
    const userProfile = await authService.getProfile(); 
    setUser({ ...userProfile, role: data.role });
    return data;
  };

  const register = async (userData) => {
    const data = await authService.register(userData);
    if (data.token) {
        localStorage.setItem('access_token', data.token);
    }
    const userProfile = await authService.getProfile();
    setUser({ ...userProfile, role: data.role });
    return data;
  };

  const logout = async () => {
    try {
      await authService.logout();
      window.location.href = '/';
    } catch (e) {
      console.error(e);
    } finally {
      localStorage.removeItem('access_token');
      setUser(null);
    }
  };

  return (
    <AuthContext.Provider value={{ user, login, register, logout, loading, isAuthenticated: !!user }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
export default AuthContext;