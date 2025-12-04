using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.API.Controllers;

[Route("api/webhook")]
[ApiController]
[AllowAnonymous] // O Webhook do Stripe não envia JWT, ele usa assinatura (Stripe-Signature)
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

        // SEGURANÇA: Chave crítica. Em produção, DEVE vir de Env Var.
        var endpointSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                             ?? _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            _logger.LogError("CRÍTICO: Stripe Webhook Secret não configurado.");
            return StatusCode(500, "Server configuration error");
        }

        try
        {
            var signature = Request.Headers["Stripe-Signature"];

            // Valida a assinatura criptográfica do Stripe
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                endpointSecret,
                throwOnApiVersionMismatch: false // Pode ajustar para true se tiver certeza da versão da API
            );

            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.Metadata != null && session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out Guid orderId))
                    {
                        var transactionId = session.PaymentIntentId;
                        _logger.LogInformation($"[Webhook] Processando pagamento para Pedido {orderId}. Transação: {transactionId}");

                        // O OrderService já cuida da idempotência (verificando se já está 'Pago')
                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId);
                    }
                    else
                    {
                        _logger.LogWarning($"[Webhook] Order ID inválido nos metadados: {orderIdString}");
                    }
                }
            }
            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
            {
                // Opcional: Implementar lógica de falha (enviar email para cliente tentar novamente)
                _logger.LogWarning($"[Webhook] Pagamento falhou: {stripeEvent.Id}");
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Erro de validação do Stripe (Assinatura inválida ou erro de API).");
            return BadRequest();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro interno ao processar Webhook.");
            return StatusCode(500);
        }
    }
}