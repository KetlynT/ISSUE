using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace GraficaModerna.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IProductRepository Products { get; }
    public ICartRepository Carts { get; }
    public IOrderRepository Orders { get; }
    public IAddressRepository Addresses { get; }
    public ICouponRepository Coupons { get; }

    public UnitOfWork(
        AppDbContext context,
        IProductRepository products,
        ICartRepository carts,
        IOrderRepository orders,
        IAddressRepository addresses,
        ICouponRepository coupons)
    {
        _context = context;
        Products = products;
        Carts = carts;
        Orders = orders;
        Addresses = addresses;
        Coupons = coupons;
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}