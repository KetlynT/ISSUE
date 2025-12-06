using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, string shippingMethod);
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto);

    // ATUALIZADO: Adicionado parâmetro amountPaidInCents para validação de segurança
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId, long amountPaidInCents);

    Task RequestRefundAsync(Guid orderId, string userId);
}