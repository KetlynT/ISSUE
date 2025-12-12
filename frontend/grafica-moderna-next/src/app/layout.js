import './globals.css';
import { Header } from '@/components/layout/Header';
import { Footer } from '@/components/layout/Footer';
import { WhatsAppButton } from '@/components/WhatsAppButton';
import { Providers } from '@/components/Providers';
import { CookieConsent } from '@/components/CookieConsent';

export const metadata = {
  title: 'Gráfica Moderna',
  description: 'Sua gráfica online de confiança',
};

export default function RootLayout({ children }) {
  return (
    <html lang="pt-BR">
      <body className="bg-gray-50 min-h-screen flex flex-col">
        <Providers>
          <Header />
          
          <main className="grow">
            {children}
          </main>

          <Footer />
          <WhatsAppButton />
          <CookieConsent />
        </Providers>
      </body>
    </html>
  );
}