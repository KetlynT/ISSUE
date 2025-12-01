using GraficaModerna.Application.DTOs;
using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Application.Interfaces;

public interface ICouponService
{
    Task<CouponResponseDto> CreateAsync(CreateCouponDto dto);
    Task<List<CouponResponseDto>> GetAllAsync();
    Task DeleteAsync(Guid id);
    Task<Coupon?> GetValidCouponAsync(string code);
}