using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace GraficaModerna.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        // CUIDADO: Isso apaga o banco para recriar com dados novos (Apenas Dev!)
        // Comente a linha abaixo se quiser persistir os dados entre reinícios
        await context.Database.EnsureDeletedAsync();

        // Garante que o banco e as tabelas (incluindo a nova UserAddresses) existam
        await context.Database.EnsureCreatedAsync();

        // --- 1. USUÁRIOS ---
        var adminEmail = config["AdminSettings:Email"] ?? "admin@graficamoderna.com";
        var adminPassword = config["AdminSettings:Password"] ?? "SenhaSegura!123";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrador Sistema",
                EmailConfirmed = true,
                PhoneNumber = "11999999999"
            };
            await userManager.CreateAsync(adminUser, adminPassword);
        }

        var clientEmail = "cliente@teste.com";
        if (await userManager.FindByEmailAsync(clientEmail) == null)
        {
            var clientUser = new ApplicationUser
            {
                UserName = clientEmail,
                Email = clientEmail,
                FullName = "João da Silva",
                EmailConfirmed = true,
                PhoneNumber = "11988887777"
            };

            var result = await userManager.CreateAsync(clientUser, "Cliente!123");

            if (result.Succeeded)
            {
                // Adiciona endereço de exemplo vinculado ao usuário
                context.UserAddresses.Add(new UserAddress
                {
                    UserId = clientUser.Id,
                    Name = "Casa",
                    ReceiverName = "João da Silva",
                    ZipCode = "01001-000",
                    Street = "Praça da Sé",
                    Number = "100",
                    Complement = "Apto 10",
                    Neighborhood = "Sé",
                    City = "São Paulo",
                    State = "SP",
                    Reference = "Próximo ao Metrô",
                    PhoneNumber = "11988887777",
                    IsDefault = true
                });
                await context.SaveChangesAsync();
            }
        }

        // --- 2. CONFIGURAÇÕES DO SITE ---
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.AddRange(
                new SiteSetting("site_name", "Gráfica A Moderna"),
                new SiteSetting("site_logo", "https://cdn-icons-png.flaticon.com/512/2972/2972461.png"),
                new SiteSetting("hero_bg_url", "https://images.unsplash.com/photo-1562564055-71e051d33c19?q=80&w=2070&auto=format&fit=crop"),
                new SiteSetting("whatsapp_number", "5511999999999"),
                new SiteSetting("whatsapp_display", "(11) 99999-9999"),
                new SiteSetting("contact_email", "contato@graficamoderna.com.br"),
                new SiteSetting("address", "Av. Paulista, 1000 - São Paulo, SP"),
                new SiteSetting("sender_cep", "01310-100"),
                new SiteSetting("hero_badge", "🚀 Qualidade Premium"),
                new SiteSetting("hero_title", "Sua marca impressa com excelência."),
                new SiteSetting("hero_subtitle", "Soluções gráficas completas para empresas e profissionais."),
                new SiteSetting("home_products_title", "Nossos Produtos"),
                new SiteSetting("home_products_subtitle", "Confira nossos produtos mais vendidos.")
            );
            await context.SaveChangesAsync();
        }

        // --- 3. CUPONS ---
        if (!context.Coupons.Any())
        {
            context.Coupons.AddRange(
                new Coupon("BEMVINDO", 10, 365),
                new Coupon("PROMO20", 20, 30),
                new Coupon("FRETEGRATIS", 100, 7)
            );
            await context.SaveChangesAsync();
        }

        // --- 4. PÁGINAS ---
        if (!context.ContentPages.Any())
        {
            context.ContentPages.AddRange(
                new ContentPage("sobre-nos", "Sobre Nós", "<p>Somos líderes de mercado em impressão digital e offset...</p>"),
                new ContentPage("politica", "Política de Privacidade", "<p>Seus dados estão protegidos conforme a LGPD...</p>")
            );
            await context.SaveChangesAsync();
        }

        // --- 5. PRODUTOS ---
        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                new Product("Cartão de Visita Premium", "Couchê 300g, laminação fosca. O cartão que impõe respeito.", 89.90m,
                    "https://images.unsplash.com/photo-1589829085413-56de8ae18c73?q=80&w=800&auto=format&fit=crop",
                    1.2m, 9, 5, 5, 500),

                new Product("Cartão de Visita Ecológico", "Papel Reciclado 240g. Sustentabilidade para sua marca.", 95.00m,
                    "https://images.unsplash.com/photo-1603201667230-bd1392185c78?q=80&w=800&auto=format&fit=crop",
                    1.1m, 9, 5, 5, 300),

                new Product("Panfletos A5 (2500 un)", "Couchê Brilho 115g. Ideal para divulgação em massa.", 149.90m,
                    "https://images.unsplash.com/photo-1586075010923-2dd45eeed8bd?q=80&w=800&auto=format&fit=crop",
                    4.0m, 21, 15, 15, 200),

                new Product("Adesivos em Vinil (m²)", "Corte especial, à prova d'água. Durabilidade externa.", 65.00m,
                    "https://images.unsplash.com/photo-1529338296731-c4280a44fc4e?q=80&w=800&auto=format&fit=crop",
                    0.5m, 30, 30, 5, 100),

                new Product("Banner em Lona 440g", "Acabamento bastão e corda. 80x120cm.", 75.00m,
                    "https://images.unsplash.com/photo-1512314889357-e157c22f938d?q=80&w=800&auto=format&fit=crop",
                    1.0m, 120, 10, 10, 50),

                new Product("Envelopes Ofício", "Papel Offset 90g. Personalizados com sua logo. 500 un.", 199.00m,
                    "https://images.unsplash.com/photo-1596230529625-7ee541fb359f?q=80&w=800&auto=format&fit=crop",
                    2.5m, 25, 15, 15, 150),

                new Product("Pasta com Bolsa", "Papel Supremo 300g. Bolsa interna colada. 100 un.", 350.00m,
                    "https://images.unsplash.com/photo-1606859187968-360523d4e8c3?q=80&w=800&auto=format&fit=crop",
                    5.0m, 35, 25, 10, 60),

                new Product("Caderno Personalizado", "Capa dura, wire-o, miolo pautado com logo.", 45.00m,
                    "https://images.unsplash.com/photo-1544816155-12df9643f363?q=80&w=800&auto=format&fit=crop",
                    0.4m, 25, 18, 2, 100)
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}