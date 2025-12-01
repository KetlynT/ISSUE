using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Recria o banco para garantir schema novo (Cuidado em produção!)
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // 1. Admin
        var adminEmail = "admin@graficamoderna.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador Sistema",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(newAdmin, "Admin@123");
        }

        // 2. Settings
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.AddRange(
                new SiteSetting("site_logo", "https://placehold.co/100x100/2563EB/ffffff?text=GM"),
                new SiteSetting("hero_bg_url", "https://images.unsplash.com/photo-1562654501-a0ccc0fc3fb1?q=80&w=1932"),
                new SiteSetting("whatsapp_number", "5511999999999"),
                new SiteSetting("whatsapp_display", "(11) 99999-9999"),
                new SiteSetting("contact_email", "contato@graficamoderna.com.br"),
                new SiteSetting("address", "Av. Paulista, 1000 - São Paulo, SP"),
                new SiteSetting("hero_badge", "🚀 A melhor gráfica da região"),
                new SiteSetting("hero_title", "Imprima suas ideias com perfeição."),
                new SiteSetting("hero_subtitle", "Cartões de visita, banners e materiais promocionais com entrega rápida."),
                new SiteSetting("home_products_title", "Nossos Produtos"),
                new SiteSetting("home_products_subtitle", "Explore nosso catálogo."),
                new SiteSetting("sender_cep", "01310-100")
            );
            await context.SaveChangesAsync();
        }

        // 3. Pages
        if (!context.ContentPages.Any())
        {
            context.ContentPages.AddRange(
                new ContentPage("sobre-nos", "Sobre a Gráfica", "<p>Texto sobre nós...</p>"),
                new ContentPage("politica", "Política de Privacidade", "<p>Termos...</p>")
            );
            await context.SaveChangesAsync();
        }

        // 4. Products (COM ESTOQUE AGORA)
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product(
                    "Cartão de Visita Premium",
                    "Papel couchê 300g, fosco, verniz localizado. 1000 un.",
                    89.90m,
                    "https://images.unsplash.com/photo-1589829085413-56de8ae18c73?q=80&w=2000&auto=format&fit=crop",
                    1.2m, 20, 10, 10,
                    100 // Estoque
                ),
                new Product(
                    "Panfletos A5 (1000 un)",
                    "Papel brilho 115g, 4x4 cores.",
                    149.00m,
                    "https://images.unsplash.com/photo-1586075010923-2dd45eeed8bd?q=80&w=2000&auto=format&fit=crop",
                    3.5m, 30, 20, 15,
                    50 // Estoque
                ),
                new Product(
                    "Banner Lona 80x120",
                    "Acabamento com bastão e corda.",
                    75.00m,
                    "https://plus.unsplash.com/premium_photo-1664302152996-2297bbdf2df4?q=80&w=2000&auto=format&fit=crop",
                    0.8m, 120, 10, 10,
                    200 // Estoque
                ),
                new Product(
                    "Adesivos Redondos",
                    "Vinil corte eletrônico.",
                    35.00m,
                    "https://images.unsplash.com/photo-1616406432452-07bc59365145?q=80&w=2000&auto=format&fit=crop",
                    0.2m, 30, 20, 2,
                    5 // Estoque Baixo (Teste)
                )
            );
            await context.SaveChangesAsync();
        }
    }
}