using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Context;

// CORREÇÃO IDE0290: Construtor Primário aplicado aqui
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
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
    public DbSet<OrderHistory> OrderHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Entity<OrderItem>().Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.Discount).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Entity<Coupon>().Property(c => c.DiscountPercentage).HasColumnType("decimal(5,2)");

        builder.Entity<Order>()
            .HasMany(o => o.History)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SiteSetting>().HasKey(s => s.Key);
    }
}