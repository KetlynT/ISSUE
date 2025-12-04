using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GraficaModerna.Infrastructure.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ?? EXEMPLOS – substitua pelos seus DbSets reais
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Cart> Carts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ------------------------------------------------------------
        // 1. APLICAR TODAS AS CONFIGURAÇÕES DO ASSEMBLY
        // ------------------------------------------------------------
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ------------------------------------------------------------
        // 2. FILTRO GLOBAL DE SOFT DELETE
        // ------------------------------------------------------------
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entity.ClrType))
            {
                var parameter = Expression.Parameter(entity.ClrType, "e");
                var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                var lambda = Expression.Lambda(compareExpression, parameter);

                entity.SetQueryFilter(lambda);
            }
        }

        // ------------------------------------------------------------
        // 3. ÍNDICES DE PERFORMANCE AUTOMÁTICOS
        // ------------------------------------------------------------
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Chave primária sempre indexada ? ignora
            foreach (var prop in entity.GetProperties())
            {
                if (prop.Name.EndsWith("Id")) // UserId, ProductId, etc
                {
                    entity.AddIndex(prop);
                }

                if (prop.ClrType == typeof(DateTime) && prop.Name.Contains("At"))
                {
                    entity.AddIndex(prop);
                }
            }
        }
    }

    // ------------------------------------------------------------
    // 4. AUDITORIA AUTOMÁTICA
    // ------------------------------------------------------------
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable)
            .ToList();

        foreach (var entry in entries)
        {
            var auditable = (IAuditable)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                auditable.UpdatedAt = DateTime.UtcNow;

                // Evita mudar CreatedAt no update
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            }

            if (entry.Entity is ISoftDelete softDel &&
                entry.State == EntityState.Deleted)
            {
                // transforma Delete ? Update com soft delete
                softDel.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
