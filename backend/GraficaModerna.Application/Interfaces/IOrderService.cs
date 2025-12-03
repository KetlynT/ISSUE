using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    // Atualizado: Agora recebe shippingCost e shippingMethod
    Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto shippingAddress, string? couponCode, decimal shippingCost, string shippingMethod);

    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode);
    Task PayOrderAsync(Guid orderId, string userId);
    Task RequestRefundAsync(Guid orderId, string userId);
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId);
}