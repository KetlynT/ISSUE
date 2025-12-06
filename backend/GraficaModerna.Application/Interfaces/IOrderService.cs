using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, string shippingMethod);

    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto);
    Task RequestRefundAsync(Guid orderId, string userId);

    // Métodos de Pagamento
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId);

    // REMOVIDO: Task PayOrderAsync(Guid orderId, string userId); -> Risco de segurança/desnecessário
}