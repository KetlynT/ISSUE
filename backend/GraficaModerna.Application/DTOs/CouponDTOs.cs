using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record CreateCouponDto(
    [Required] string Code,
    [Range(1, 100)] decimal DiscountPercentage,
    [Range(1, 3650)] int ValidityDays
);

public record CouponResponseDto(
    Guid Id,
    string Code,
    decimal DiscountPercentage,
    DateTime ExpiryDate,
    bool IsActive
);