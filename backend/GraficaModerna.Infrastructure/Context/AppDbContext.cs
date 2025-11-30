using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Necessário para Identity
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Context;

// CORRIGIDO: Herdar de IdentityDbContext<ApplicationUser> em vez de DbContext simples
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<ContentPage> ContentPages { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Importante para o Identity configurar as tabelas dele

        builder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.Property(p => p.IsActive).HasDefaultValue(true);
        });

        builder.Entity<ContentPage>(e => {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Slug).IsUnique();
        });

        builder.Entity<SiteSetting>(e => {
            e.HasKey(s => s.Key);
        });
    }
}