using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, string shippingMethod);
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto);

    // Certifique-se que esta linha tem os 3 parâmetros (incluindo long amountPaidInCents)
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId, long amountPaidInCents);

    // Certifique-se de que NÃO existe um método "PayOrderAsync" aqui, pois ele não existe no Service
    Task RequestRefundAsync(Guid orderId, string userId);
}