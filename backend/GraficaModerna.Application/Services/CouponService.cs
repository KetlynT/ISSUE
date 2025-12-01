using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;

namespace GraficaModerna.Application.Services;

public class CouponService : ICouponService
{
    private readonly AppDbContext _context;

    public CouponService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CouponResponseDto> CreateAsync(CreateCouponDto dto)
    {
        if (await _context.Coupons.AnyAsync(c => c.Code == dto.Code.ToUpper()))
            throw new Exception("Cupom já existe.");

        var coupon = new Coupon(dto.Code, dto.DiscountPercentage, dto.ValidityDays);
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();

        return new CouponResponseDto(coupon.Id, coupon.Code, coupon.DiscountPercentage, coupon.ExpiryDate, coupon.IsActive);
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
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper());
        if (coupon == null || !coupon.IsValid()) return null;
        return coupon;
    }
}