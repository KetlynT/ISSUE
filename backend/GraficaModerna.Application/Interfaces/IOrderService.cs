using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    // O Checkout é o processo de transformar um Carrinho em um Pedido
    Task<OrderDto> CreateOrderFromCartAsync(string userId, string shippingAddress, string shippingZip, string? couponCode);

    // Consultas de Pedidos
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();

    // Gestão de Status (Atualizado para receber TrackingCode)
    Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode);
}