import React, { useEffect, Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

import { MainLayout } from './components/layout/MainLayout';
import { ContentService } from './services/contentService';
import { CartProvider } from './context/CartContext';
import { AuthProvider, useAuth } from './context/AuthContext'; // NOVO
import ScrollToTop from './components/ScrollToTop';
import { CookieConsent } from './components/CookieConsent';

// Lazy Loading
const Home = lazy(() => import('./pages/Home').then(module => ({ default: module.Home })));
const Login = lazy(() => import('./pages/Login').then(module => ({ default: module.Login })));
const Register = lazy(() => import('./pages/Register').then(module => ({ default: module.Register })));
const AdminDashboard = lazy(() => import('./pages/AdminDashboard').then(module => ({ default: module.AdminDashboard })));
const AdminLogin = lazy(() => import('./pages/AdminLogin').then(module => ({ default: module.AdminLogin })));
const ProductDetails = lazy(() => import('./pages/ProductDetails').then(module => ({ default: module.ProductDetails })));
const GenericPage = lazy(() => import('./pages/GenericPage').then(module => ({ default: module.GenericPage })));
const Contact = lazy(() => import('./pages/Contact').then(module => ({ default: module.Contact })));
const Cart = lazy(() => import('./pages/Cart').then(module => ({ default: module.Cart })));
const Checkout = lazy(() => import('./pages/Checkout').then(module => ({ default: module.Checkout })));
const MyOrders = lazy(() => import('./pages/MyOrders').then(module => ({ default: module.MyOrders })));
const Profile = lazy(() => import('./pages/Profile').then(module => ({ default: module.Profile })));
const Success = lazy(() => import('./pages/Success').then(module => ({ default: module.Success })));
const ErrorPage = lazy(() => import('./pages/ErrorPage').then(module => ({ default: module.ErrorPage })));

const PageLoader = () => (
  <div className="min-h-screen flex items-center justify-center bg-gray-50">
    <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div>
  </div>
);

// Rota Privada Atualizada: Espera o AuthContext carregar
const PrivateRoute = ({ children }) => {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) return <PageLoader />;
  
  if (!user) {
    // Redireciona para login salvando a origem
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
};

// Rota Pública (Login/Register): Se já logado, redireciona
const PublicOnlyRoute = ({ children }) => {
  const { user, loading } = useAuth();
  
  if (loading) return <PageLoader />;

  if (user) {
    if (user.role === 'Admin') return <Navigate to="/putiroski/dashboard" replace />;
    return <Navigate to="/" replace />;
  }
  return children;
};

const setFallbackFavicon = () => {
    const link = document.querySelector("link[rel~='icon']") || document.createElement('link');
    link.type = 'image/png'; link.rel = 'icon'; link.href = '/favicon.ico'; // Simplificado
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
    <AuthProvider>
      <CartProvider>
        <BrowserRouter>
          <ScrollToTop />
          <Toaster position="top-right" />
          <CookieConsent />
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route path="/error" element={<ErrorPage />} />

              <Route element={<MainLayout />}>
                <Route path="/" element={<Home />} />
                <Route path="/contato" element={<Contact />} />
                <Route path="/produto/:id" element={<ProductDetails />} />
                <Route path="/pagina/:slug" element={<GenericPage />} />
                <Route path="/carrinho" element={<Cart />} />
                
                {/* Rotas Protegidas */}
                <Route path="/checkout" element={<PrivateRoute><Checkout /></PrivateRoute>} />
                <Route path="/meus-pedidos" element={<PrivateRoute><MyOrders /></PrivateRoute>} />
                <Route path="/perfil" element={<PrivateRoute><Profile /></PrivateRoute>} />
                <Route path="/sucesso" element={<PrivateRoute><Success /></PrivateRoute>} />
              </Route>

              {/* Rotas de Acesso */}
              <Route path="/login" element={<PublicOnlyRoute><Login /></PublicOnlyRoute>} />
              <Route path="/cadastro" element={<PublicOnlyRoute><Register /></PublicOnlyRoute>} />
              
              {/* Admin */}
              <Route path="/putiroski" element={<PublicOnlyRoute><AdminLogin /></PublicOnlyRoute>} />
              <Route path="/putiroski/dashboard" element={<PrivateRoute><AdminDashboard /></PrivateRoute>} />

              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Suspense>
        </BrowserRouter>
      </CartProvider>
    </AuthProvider>
  );
}

export default App;