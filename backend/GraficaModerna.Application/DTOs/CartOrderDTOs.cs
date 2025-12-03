using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

// --- CARRINHO ---
public record AddToCartDto(Guid ProductId, [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser no mínimo 1.")] int Quantity);
public record UpdateCartItemDto([Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser no mínimo 1.")] int Quantity);
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

// --- PEDIDOS ---
public record OrderDto(
    Guid Id,
    DateTime OrderDate,
    DateTime? DeliveryDate,
    decimal TotalAmount,
    string Status,
    string? TrackingCode,
    // NOVOS CAMPOS DE VISUALIZAÇÃO
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string ShippingAddress,
    List<OrderItemDto> Items
);

public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);

// DTO DE ATUALIZAÇÃO DO ADMIN
public record UpdateOrderStatusDto(
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode, // Admin pode enviar isso agora
    string? ReturnInstructions    // E isso
);