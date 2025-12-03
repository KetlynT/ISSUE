using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly AppDbContext _context;
    public CouponRepository(AppDbContext context) { _context = context; }

    public async Task<Coupon?> GetByCodeAsync(string code) => await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper());

    public async Task<List<Coupon>> GetAllAsync() => await _context.Coupons.OrderByDescending(c => c.ExpiryDate).ToListAsync();

    public async Task AddAsync(Coupon coupon) => await _context.Coupons.AddAsync(coupon);

    public async Task DeleteAsync(Guid id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon != null) _context.Coupons.Remove(coupon);
    }
}