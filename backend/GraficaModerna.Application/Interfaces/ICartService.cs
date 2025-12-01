using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task AddItemAsync(string userId, AddToCartDto dto);
    Task RemoveItemAsync(string userId, Guid cartItemId);
    Task ClearCartAsync(string userId);
    // Checkout atualizado para aceitar cupom
    Task<OrderDto> CheckoutAsync(string userId, string shippingAddress, string shippingZip, string? couponCode);
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task UpdateOrderStatusAsync(Guid orderId, string status);
}