import React, { useEffect, Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

import { MainLayout } from './components/layout/MainLayout';
import { AuthService } from './services/authService';
import { ContentService } from './services/contentService';
import { CartProvider } from './context/CartContext';
import ScrollToTop from './components/ScrollToTop';

// Lazy Loading
const Home = lazy(() => import('./pages/Home').then(module => ({ default: module.Home })));
const Login = lazy(() => import('./pages/Login').then(module => ({ default: module.Login })));
const Register = lazy(() => import('./pages/Register').then(module => ({ default: module.Register })));
const AdminDashboard = lazy(() => import('./pages/AdminDashboard').then(module => ({ default: module.AdminDashboard })));
const AdminLogin = lazy(() => import('./pages/AdminLogin').then(module => ({ default: module.AdminLogin }))); // NOVO IMPORT
const ProductDetails = lazy(() => import('./pages/ProductDetails').then(module => ({ default: module.ProductDetails })));
const GenericPage = lazy(() => import('./pages/GenericPage').then(module => ({ default: module.GenericPage })));
const Contact = lazy(() => import('./pages/Contact').then(module => ({ default: module.Contact })));
const Cart = lazy(() => import('./pages/Cart').then(module => ({ default: module.Cart })));
const Checkout = lazy(() => import('./pages/Checkout').then(module => ({ default: module.Checkout })));
const MyOrders = lazy(() => import('./pages/MyOrders').then(module => ({ default: module.MyOrders })));
const Profile = lazy(() => import('./pages/Profile').then(module => ({ default: module.Profile })));

const PageLoader = () => (
  <div className="min-h-screen flex items-center justify-center bg-gray-50">
    <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div>
  </div>
);

const PrivateRoute = ({ children }) => {
  const isAuth = AuthService.isAuthenticated();
  return isAuth ? children : <Navigate to="/login" replace />;
};

// Rota para bloquear usuários logados de acessar login de novo
const PublicOnlyRoute = ({ children }) => {
  const isAuth = AuthService.isAuthenticated();
  if (isAuth) {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    // Se for Admin, manda pro Dashboard secreto
    if (user.role === 'Admin') return <Navigate to="/putiroski/dashboard" replace />;
    // Se for User, manda pra Home
    return <Navigate to="/" replace />;
  }
  return children;
};

// --- FAVICON SETUP ---
const setFallbackFavicon = () => {
    const canvas = document.createElement('canvas');
    canvas.width = 64;
    canvas.height = 64;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#1f2937';
    ctx.beginPath(); ctx.roundRect(0, 0, 64, 64, 16); ctx.fill();
    ctx.font = 'bold 40px sans-serif'; ctx.fillStyle = 'white'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText('X', 32, 34);
    const link = document.querySelector("link[rel~='icon']") || document.createElement('link');
    link.type = 'image/png'; link.rel = 'icon'; link.href = canvas.toDataURL();
    document.getElementsByTagName('head')[0].appendChild(link);
};

function App() {
  useEffect(() => {
    const updateSiteIdentity = async () => {
      try {
        const settings = await ContentService.getSettings();
        if (settings?.site_name) document.title = settings.site_name;
        if (settings?.site_logo) {
          let link = document.querySelector("link[rel~='icon']");
          if (!link) { link = document.createElement('link'); link.rel = 'icon'; document.getElementsByTagName('head')[0].appendChild(link); }
          link.href = settings.site_logo;
        } else { setFallbackFavicon(); }
      } catch (error) { setFallbackFavicon(); }
    };
    updateSiteIdentity();
  }, []);

  return (
    <CartProvider>
      <BrowserRouter>
        <ScrollToTop />
        <Toaster position="top-right" />
        <Suspense fallback={<PageLoader />}>
          <Routes>
            {/* ROTAS PÚBLICAS (SITE) */}
            <Route element={<MainLayout />}>
              <Route path="/" element={<Home />} />
              <Route path="/contato" element={<Contact />} />
              <Route path="/produto/:id" element={<ProductDetails />} />
              <Route path="/pagina/:slug" element={<GenericPage />} />
              <Route path="/carrinho" element={<Cart />} />
              <Route path="/checkout" element={<PrivateRoute><Checkout /></PrivateRoute>} />
              <Route path="/meus-pedidos" element={<PrivateRoute><MyOrders /></PrivateRoute>} />
              <Route path="/perfil" element={<PrivateRoute><Profile /></PrivateRoute>} />
            </Route>

            {/* ROTA LOGIN CLIENTE */}
            <Route path="/login" element={<PublicOnlyRoute><Login /></PublicOnlyRoute>} />
            <Route path="/cadastro" element={<PublicOnlyRoute><Register /></PublicOnlyRoute>} />
            
            {/* ÁREA SECRETA ADMIN */}
            {/* 1. Login Secreto */}
            <Route path="/putiroski" element={<PublicOnlyRoute><AdminLogin /></PublicOnlyRoute>} />
            
            {/* 2. Dashboard Secreto (Protegido) */}
            <Route path="/putiroski/dashboard" element={<PrivateRoute><AdminDashboard /></PrivateRoute>} />
            
            {/* Compatibilidade com links antigos (se houver) */}
            <Route path="/painel-restrito-gerencial" element={<Navigate to="/putiroski/dashboard" replace />} />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </CartProvider>
  );
}

export default App;