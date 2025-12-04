using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.API.Controllers;

[Route("api/webhook")]
[ApiController]
public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IOrderService _orderService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IConfiguration configuration,
        IOrderService orderService,
        ILogger<StripeWebhookController> logger)
    {
        _configuration = configuration;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeEvent()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        // Tenta pegar o segredo do .env ou User Secrets
        var endpointSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            _logger.LogError("Webhook Secret não configurado no servidor.");
            return StatusCode(500);
        }

        try
        {
            var signature = Request.Headers["Stripe-Signature"];

            // CORREÇÃO AQUI: throwOnApiVersionMismatch: false
            // Isso permite que o Stripe envie eventos de uma versão mais nova sem quebrar o backend
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                endpointSecret,
                throwOnApiVersionMismatch: false
            );

            // 2. Processa apenas o evento de Checkout Completado
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                // Conversão segura com o 'as'
                var session = stripeEvent.Data.Object as Session;

                // Verifica se temos os metadados que enviamos na criação da sessão
                if (session != null && session.Metadata != null && session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out Guid orderId))
                    {
                        var transactionId = session.PaymentIntentId;

                        _logger.LogInformation($"Webhook recebido! Pagamento confirmado para o Pedido {orderId}. Transação: {transactionId}");

                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId);
                    }
                    else
                    {
                        _logger.LogError($"ID de pedido inválido nos metadados do Webhook: {orderIdString}");
                    }
                }
                else
                {
                    _logger.LogWarning("Webhook recebido sem Metadata 'order_id'.");
                }
            }
            else
            {
                // Outros eventos
                // _logger.LogInformation($"Evento ignorado: {stripeEvent.Type}");
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Erro de validação no Webhook Stripe.");
            return BadRequest();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro interno ao processar Webhook.");
            return StatusCode(500);
        }
    }
}