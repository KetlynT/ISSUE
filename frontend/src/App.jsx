import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';

import { MainLayout } from './components/layout/MainLayout';
import { Home } from './pages/Home';
import { Login } from './pages/Login';
import { AdminDashboard } from './pages/AdminDashboard';
import { ProductDetails } from './pages/ProductDetails';
import { GenericPage } from './pages/GenericPage';
import { Contact } from './pages/Contact'; // <--- IMPORT NOVO
import { AuthService } from './services/authService';

const PrivateRoute = ({ children }) => {
  return AuthService.isAuthenticated() ? children : <Navigate to="/login" />;
};

function App() {
  return (
    <BrowserRouter>
      <Toaster position="top-right" />
      <Routes>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Home />} />
          <Route path="/contato" element={<Contact />} /> {/* <--- ROTA NOVA */}
          <Route path="/produto/:id" element={<ProductDetails />} />
          
          {/* Rota Dinâmica para páginas de conteúdo */}
          <Route path="/pagina/:slug" element={<GenericPage />} />
        </Route>

        <Route path="/login" element={<Login />} />
        
        <Route 
          path="/admin" 
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