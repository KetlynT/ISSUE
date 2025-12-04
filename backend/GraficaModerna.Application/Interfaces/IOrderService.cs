using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto shippingAddress, string? couponCode, decimal shippingCost, string shippingMethod);

    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();

    // Método simplificado (Mantido para compatibilidade)
    Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode);

    // NOVO: Método completo para o Painel Admin (permite Logística Reversa)
    Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto);

    Task PayOrderAsync(Guid orderId, string userId);
    Task RequestRefundAsync(Guid orderId, string userId);
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId);
}