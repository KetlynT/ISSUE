using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.API.Data;

public static class DbSeeder
{
    // Adicione UserManager ao método
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Usuário Admin (CRUCIAL)
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

        // 2. Seed Settings
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.AddRange(
                new SiteSetting("whatsapp_number", "5511999999999"),
                new SiteSetting("whatsapp_display", "(11) 99999-9999"),
                new SiteSetting("contact_email", "contato@graficamoderna.com.br"),
                new SiteSetting("address", "Av. Paulista, 1000 - São Paulo, SP")
            );
        }

        // 3. Seed Pages
        if (!context.ContentPages.Any())
        {
            context.ContentPages.AddRange(
                new ContentPage("sobre-nos", "Sobre a Gráfica A Moderna",
                    "<p>Desde 2024 entregando qualidade...</p>"),
                new ContentPage("politica-privacidade", "Política de Privacidade",
                    "<p>Seus dados estão seguros...</p>")
            );
        }

        await context.SaveChangesAsync();
    }
}