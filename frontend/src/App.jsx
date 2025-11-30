import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

import { MainLayout } from './components/layout/MainLayout';
import { Home } from './pages/Home';
import { Login } from './pages/Login';
import { AdminDashboard } from './pages/AdminDashboard';
import { ProductDetails } from './pages/ProductDetails';
import { GenericPage } from './pages/GenericPage';
import { Contact } from './pages/Contact';
import { AuthService } from './services/authService';
import { ContentService } from './services/contentService'; // Importar serviço
import ScrollToTop from './components/ScrollToTop';

const PrivateRoute = ({ children }) => {
  const isAuth = AuthService.isAuthenticated();
  return isAuth ? children : <Navigate to="/login" replace />;
};

const PublicOnlyRoute = ({ children }) => {
  const isAuth = AuthService.isAuthenticated();
  return isAuth ? <Navigate to="/painel-restrito-gerencial" replace /> : children;
};

function App() {
  
  // Efeito para carregar o FAVICON do banco de dados
  useEffect(() => {
    const updateFavicon = async () => {
      try {
        const settings = await ContentService.getSettings();
        if (settings && settings.site_logo) {
          // Encontra a tag do favicon existente ou cria uma nova
          let link = document.querySelector("link[rel~='icon']");
          if (!link) {
            link = document.createElement('link');
            link.rel = 'icon';
            document.getElementsByTagName('head')[0].appendChild(link);
          }
          // Atualiza a URL do favicon
          link.href = settings.site_logo;
        }
      } catch (error) {
        console.error("Erro ao atualizar favicon:", error);
      }
    };

    updateFavicon();
  }, []);

  return (
    <BrowserRouter>
      <ScrollToTop />
      
      <Toaster position="top-right" />
      <Routes>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Home />} />
          <Route path="/contato" element={<Contact />} />
          <Route path="/produto/:id" element={<ProductDetails />} />
          <Route path="/pagina/:slug" element={<GenericPage />} />
        </Route>

        <Route 
          path="/login" 
          element={
            <PublicOnlyRoute>
              <Login />
            </PublicOnlyRoute>
          } 
        />
        
        <Route 
          path="/painel-restrito-gerencial" 
          element={
            <PrivateRoute>
              <AdminDashboard />
            </PrivateRoute>
          } 
        />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;