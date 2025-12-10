using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;


public class CouponService(AppDbContext context) : ICouponService
{
    private readonly AppDbContext _context = context;

    public async Task<CouponResponseDto> CreateAsync(CreateCouponDto dto)
    {
        if (await _context.Coupons.AnyAsync(c => c.Code.Equals(dto.Code, StringComparison.CurrentCultureIgnoreCase)))
            throw new Exception("Cupom j� existe.");

        var coupon = new Coupon(dto.Code, dto.DiscountPercentage, dto.ValidityDays);
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();

        return new CouponResponseDto(coupon.Id, coupon.Code, coupon.DiscountPercentage, coupon.ExpiryDate,
            coupon.IsActive);
    }

    public async Task<List<CouponResponseDto>> GetAllAsync()
    {
        return await _context.Coupons
            .OrderByDescending(c => c.ExpiryDate)
            .Select(c => new CouponResponseDto(c.Id, c.Code, c.DiscountPercentage, c.ExpiryDate, c.IsActive))
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Coupon?> GetValidCouponAsync(string code)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;
        if (elapsed < 300) 
        {
            await Task.Delay((int)(300 - elapsed));
        }
        if (coupon == null || !coupon.IsValid()) return null;
        return coupon;
    }
}
