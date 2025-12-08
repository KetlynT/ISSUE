using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record AddToCartDto(
    Guid ProductId,
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser no mínimo 1.")]
    int Quantity);

public record UpdateCartItemDto(
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser no mínimo 1.")]
    int Quantity);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductImage,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    decimal Weight,
    int Width,
    int Height,
    int Length
);

public record CartDto(Guid Id, List<CartItemDto> Items, decimal GrandTotal);

public record OrderDto(
    Guid Id,
    DateTime OrderDate,
    DateTime? DeliveryDate,
    decimal SubTotal,
    decimal Discount,
    decimal ShippingCost,
    decimal TotalAmount,
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string? RefundRejectionReason,
    string? RefundRejectionProof,
    string ShippingAddress,
    string CustomerName,
    string CustomerCpf,
    string CustomerEmail,
    List<OrderItemDto> Items
);

public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);

public record UpdateOrderStatusDto(
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string? RefundRejectionReason,
    string? RefundRejectionProof
);