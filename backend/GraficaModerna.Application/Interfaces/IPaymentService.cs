using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Cria uma sessão de checkout no Stripe e retorna a URL para redirecionamento.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(Order order);
}