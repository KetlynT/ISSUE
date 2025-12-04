using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(Order order);

    // NOVO: Método para processar reembolso total
    Task RefundPaymentAsync(string paymentIntentId);
}