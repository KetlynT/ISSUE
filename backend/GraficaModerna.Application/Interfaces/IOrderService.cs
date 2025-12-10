using GraficaModerna.Application.DTOs;
using GraficaModerna.Domain.Models;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(
        string userId, 
        CreateAddressDto addressDto, 
        string? couponCode,
        string shippingMethod);

    Task<PagedResultDto<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<PagedResultDto<AdminOrderDto>> GetAllOrdersAsync(int page, int pageSize);
    Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto);
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId, long amountPaidInCents);
    Task RequestRefundAsync(Guid orderId, string userId, RequestRefundDto dto);
}