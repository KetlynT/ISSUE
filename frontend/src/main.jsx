import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'

class GlobalErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true };
  }

  componentDidCatch(error, errorInfo) {
    console.error("Erro Crítico da Aplicação:", error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
          <div className="text-center space-y-4">
            <h1 className="text-2xl font-bold text-gray-800">Ops! Algo deu errado.</h1>
            <p className="text-gray-600">Tivemos um problema inesperado na aplicação.</p>
            <button 
              onClick={() => {
                // Lógica de Preservação aqui também
                const savedConsent = localStorage.getItem('lgpd_consent');
                localStorage.clear();
                if (savedConsent) localStorage.setItem('lgpd_consent', savedConsent);
                
                window.location.href = '/';
              }}
              className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
            >
              Recarregar Página
            </button>
          </div>
        </div>
      );
    }

    return this.props.children; 
  }
}

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <GlobalErrorBoundary>
      <App />
    </GlobalErrorBoundary>
  </React.StrictMode>,
)