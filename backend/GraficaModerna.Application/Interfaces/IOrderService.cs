using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IOrderService
{
    // O Checkout é o processo de transformar um Carrinho em um Pedido
    Task<OrderDto> CreateOrderFromCartAsync(string userId, string shippingAddress, string shippingZip, string? couponCode);

    // Consultas de Pedidos
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);
    Task<List<OrderDto>> GetAllOrdersAsync();

    // Gestão de Status (Uso Admin)
    Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode);

    // NOVO: Método seguro para pagamento (Uso Cliente)
    // Exige o userId para validar a propriedade do pedido
    Task PayOrderAsync(Guid orderId, string userId);

    // Cliente solicita reembolso
    Task RequestRefundAsync(Guid orderId, string userId);
    Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId); // NOVO: Método do Sistema (Prod)
}