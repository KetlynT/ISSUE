import React, { useEffect, useState, Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

import { MainLayout } from './components/layout/MainLayout';
import { ContentService } from './services/contentService';
import { CartProvider } from './context/CartContext';
import { AuthProvider, useAuth } from './context/AuthContext';
import ScrollToTop from './components/ScrollToTop';
import { CookieConsent } from './components/CookieConsent';

// Lazy load pages
const Home = lazy(() => import('./pages/Home').then(module => ({ default: module.Home })));
const Login = lazy(() => import('./pages/Login').then(module => ({ default: module.Login })));
const Register = lazy(() => import('./pages/Register').then(module => ({ default: module.Register })));
const ForgotPassword = lazy(() => import('./pages/ForgotPassword').then(module => ({ default: module.ForgotPassword })));
const ResetPassword = lazy(() => import('./pages/ResetPassword').then(module => ({ default: module.ResetPassword })));
const ConfirmEmail = lazy(() => import('./pages/ConfirmEmail').then(module => ({ default: module.ConfirmEmail })));
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

const PrivateRoute = ({ children }) => {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) return <PageLoader />;
  
  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
};

const PublicOnlyRoute = ({ children }) => {
  const { user, loading } = useAuth();
  
  if (loading) return <PageLoader />;

  if (user) {
    if (user.role === 'Admin') return <Navigate to="/putiroski/dashboard" replace />;
    return <Navigate to="/" replace />;
  }
  return children;
};

const PurchaseGuard = ({ children }) => {
    const [enabled, setEnabled] = useState(null);
    
    useEffect(() => {
        const check = async () => {
            try {
                const settings = await ContentService.getSettings();
                setEnabled(settings?.purchase_enabled !== 'false');
            } catch {
                setEnabled(true);
            }
        };
        check();
    }, []);

    if (enabled === null) return <PageLoader />;
    if (!enabled) return <Navigate to="/" replace />;
    
    return children;
};

const setFallbackFavicon = () => {
    const link = document.querySelector("link[rel~='icon']") || document.createElement('link');
    link.type = 'image/png'; link.rel = 'icon'; link.href = '/favicon.ico';
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
        const root = document.documentElement;
        if (settings?.primary_color) root.style.setProperty('--color-primary', settings.primary_color);
        if (settings?.secondary_color) root.style.setProperty('--color-secondary', settings.secondary_color);
        if (settings?.footer_bg_color) root.style.setProperty('--color-footer-bg', settings.footer_bg_color);
        if (settings?.footer_text_color) root.style.setProperty('--color-footer-text', settings.footer_text_color);
      } catch (error) { setFallbackFavicon(); }
    };
    updateSiteIdentity();
  }, []);

  return (
    <AuthProvider>
      <CartProvider>
        <BrowserRouter>
          <ScrollToTop />
          <Toaster position="top-right" containerStyle={{top: 80,}}/>
          <CookieConsent />
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route path="/error" element={<ErrorPage />} />

              <Route element={<MainLayout />}>
                <Route path="/" element={<Home />} />
                <Route path="/contato" element={<Contact />} />
                <Route path="/produto/:id" element={<ProductDetails />} />
                <Route path="/pagina/:slug" element={<GenericPage />} />
                
                <Route path="/carrinho" element={<PurchaseGuard><Cart /></PurchaseGuard>} />
                
                <Route path="/checkout" element={<PrivateRoute><PurchaseGuard><Checkout /></PurchaseGuard></PrivateRoute>} />
                <Route path="/meus-pedidos" element={<PrivateRoute><MyOrders /></PrivateRoute>} />
                <Route path="/perfil" element={<PrivateRoute><Profile /></PrivateRoute>} />
                <Route path="/sucesso" element={<PrivateRoute><Success /></PrivateRoute>} />
              </Route>

              <Route path="/login" element={<PublicOnlyRoute><Login /></PublicOnlyRoute>} />
              <Route path="/esqueci-senha" element={<PublicOnlyRoute><ForgotPassword /></PublicOnlyRoute>} />
              <Route path="/reset-password" element={<PublicOnlyRoute><ResetPassword /></PublicOnlyRoute>} />
              <Route path="/confirm-email" element={<PublicOnlyRoute><ConfirmEmail /></PublicOnlyRoute>} />

              <Route path="/cadastro" element={<PublicOnlyRoute><PurchaseGuard><Register /></PurchaseGuard></PublicOnlyRoute>} />
              
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