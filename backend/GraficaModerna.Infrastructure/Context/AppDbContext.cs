using System.ComponentModel;
using System.Globalization;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Enums;
using GraficaModerna.Domain.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
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
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(u => u.CpfCnpj)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Entity<Product>()
            .HasIndex(p => new { p.IsActive, p.Price })
            .HasDatabaseName("IX_Product_IsActive_Price");

        builder.Entity<OrderItem>().Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.Discount).HasColumnType("decimal(18,2)");
        builder.Entity<Order>().Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .ToTable(t => t.HasCheckConstraint("CK_Order_TotalAmount",
                $"\"TotalAmount\" >= {Order.MinOrderAmount.ToString(CultureInfo.InvariantCulture)} AND \"TotalAmount\" <= {Order.MaxOrderAmount.ToString(CultureInfo.InvariantCulture)}"));

        builder.Entity<Order>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_Order_Status");

        builder.Entity<Order>()
            .HasIndex(o => new { o.UserId, o.OrderDate })
            .HasDatabaseName("IX_Order_UserId_OrderDate");

        builder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion(
                v => v.GetDescription(),
                v => ParseStatus(v)
            );

        builder.Entity<Coupon>().Property(c => c.DiscountPercentage).HasColumnType("decimal(5,2)");

        builder.Entity<Coupon>()
            .HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_Coupon_Code");

        builder.Entity<Coupon>()
            .ToTable(t => t.HasCheckConstraint(
            "CK_Coupon_DiscountPercentage",
            "\"DiscountPercentage\" > 0 AND \"DiscountPercentage\" <= 100"));

        builder.Entity<CouponUsage>()
            .HasIndex(cu => new { cu.UserId, cu.CouponCode })
            .IsUnique()
            .HasDatabaseName("IX_CouponUsage_UserId_CouponCode");

        builder.Entity<Order>()
            .HasMany(o => o.History)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SiteSetting>().HasKey(s => s.Key);

        builder.Entity<ProcessedWebhookEvent>().HasKey(e => e.EventId);
    }

    private static OrderStatus ParseStatus(string status)
    {
        foreach (var field in typeof(OrderStatus).GetFields())
        {
            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            if (attribute != null && attribute.Description == status)
                return (OrderStatus)field.GetValue(null)!;

            if (field.Name == status)
                return (OrderStatus)field.GetValue(null)!;
        }
        return OrderStatus.Pendente;
    }
}