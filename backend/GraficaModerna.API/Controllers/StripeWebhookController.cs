using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.API.Controllers;

[Route("api/webhook")]
[ApiController]
[AllowAnonymous]
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
            var stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret, throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.Metadata != null && session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out Guid orderId))
                    {
                        var transactionId = session.PaymentIntentId;

                        // CORREÇÃO: Capturamos o valor pago (em centavos) para validação
                        // Se for nulo, assumimos 0 para garantir que falhe na validação
                        long amountPaid = session.AmountTotal ?? 0;

                        _logger.LogInformation($"[Webhook] Processando pagamento para Pedido {orderId}. Transação: {transactionId}. Valor: {amountPaid}");

                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                    }
                    else
                    {
                        _logger.LogWarning($"[Webhook] Order ID inválido nos metadados: {orderIdString}");
                    }
                }
            }
            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
            {
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