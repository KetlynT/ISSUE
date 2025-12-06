using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entidades do Negócio
    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponUsage> CouponUsages { get; set; }
    public DbSet<ContentPage> ContentPages { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }

    // NOVO: Tabela de Auditoria de Pedidos
    public DbSet<OrderHistory> OrderHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuração precisa para Decimal (PostgreSQL/SQLServer)
        // Evita warnings e erros de truncamento em valores monetários

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.Discount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.ShippingCost)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Coupon>()
            .Property(c => c.DiscountPercentage)
            .HasColumnType("decimal(5,2)");

        // Configuração de relacionamento para Auditoria (Opcional, mas recomendado)
        builder.Entity<Order>()
            .HasMany(o => o.History)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}