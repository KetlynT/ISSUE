namespace GraficaModerna.Application.DTOs;

// --- CARRINHO ---
public record AddToCartDto(Guid ProductId, int Quantity);

public record UpdateCartItemDto(int Quantity); // NOVO: DTO para atualizar quantidade

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductImage,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    // NOVOS CAMPOS PARA FRETE
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
    decimal TotalAmount,
    string Status,
    string ShippingAddress,
    List<OrderItemDto> Items
);

public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);