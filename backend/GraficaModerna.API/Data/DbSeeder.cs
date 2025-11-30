using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Usuário Admin
        var adminEmail = "admin@graficamoderna.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador Sistema",
                EmailConfirmed = true
            };

            // Senha forte padrão (altere no primeiro login)
            await userManager.CreateAsync(newAdmin, "Admin@123");
        }

        // 2. Seed Settings (Incluindo Hero e Contato)
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.AddRange(
                // Configurações de Contato
                new SiteSetting("whatsapp_number", "5511999999999"),
                new SiteSetting("whatsapp_display", "(11) 99999-9999"),
                new SiteSetting("contact_email", "contato@graficamoderna.com.br"),
                new SiteSetting("address", "Av. Paulista, 1000 - São Paulo, SP"),

                // Configurações da Home (Hero Section)
                new SiteSetting("hero_badge", "🚀 A melhor gráfica da região"),
                new SiteSetting("hero_title", "Imprima suas ideias com perfeição."),
                new SiteSetting("hero_subtitle", "Cartões de visita, banners e materiais promocionais com entrega rápida e qualidade premium.")
            );
        }

        // 3. Seed Pages
        if (!context.ContentPages.Any())
        {
            context.ContentPages.AddRange(
                new ContentPage("sobre-nos", "Sobre a Gráfica A Moderna",
                    "<p>Desde 2024 entregando qualidade e excelência em impressão...</p>"),
                new ContentPage("politica-privacidade", "Política de Privacidade",
                    "<p>Nós valorizamos seus dados. Esta política descreve como...</p>")
            );
        }

        await context.SaveChangesAsync();
    }
}