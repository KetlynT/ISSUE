using System.Net;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.API.Controllers;

[Route("api/webhook")]
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("WebhookPolicy")]
public class StripeWebhookController : ControllerBase
{
    private readonly HashSet<string> _authorizedIps;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly IOrderService _orderService;

    public StripeWebhookController(
        IConfiguration configuration,
        IOrderService orderService,
        ILogger<StripeWebhookController> logger)
    {
        _configuration = configuration;
        _orderService = orderService;
        _logger = logger;

        var ipsString = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_IPS")
                        ?? _configuration["STRIPE_WEBHOOK_IPS"];

        if (!string.IsNullOrEmpty(ipsString))
        {
            var ips = ipsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _authorizedIps = [.. ips]; 
        }
        else
        {
            _authorizedIps = []; 
            _logger.LogWarning("ALERTA: 'STRIPE_WEBHOOK' não configurado.");
        }
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeEvent()
    {
        if (!IsRequestFromStripe(HttpContext.Connection.RemoteIpAddress))
        {
            _logger.LogWarning("[Webhook] Bloqueado");
            return StatusCode(403, "Forbidden: Source Denied");
        }

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                             ?? _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            _logger.LogError("CRÍTICO: Stripe Webhook não configurado.");
            return StatusCode(500);
        }

        try
        {
            var signature = Request.Headers["Stripe-Signature"];
            var stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret);

            // CORREÇÃO: Verificar explicitamente o tipo do evento
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                // A lógica de confirmação só deve ocorrer se o checkout foi completado com sucesso
                if (stripeEvent.Data.Object is Session session &&
                    session.Metadata != null &&
                    session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out var orderId))
                    {
                        var transactionId = session.PaymentIntentId;
                        var amountPaid = session.AmountTotal ?? 0;

                        _logger.LogInformation(
                            "[Webhook] Processando pagamento para Pedido {OrderId}. Transação: {TransactionId}. Valor: {AmountPaid}",
                            orderId, transactionId, amountPaid);

                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                    }
                    else
                    {
                        _logger.LogWarning("[Webhook] Order ID inválido nos metadados: {OrderIdString}", orderIdString);
                    }
                }
            }
            // Adicionado tratamento para pagamento assíncrono com sucesso, se necessário
            else if (stripeEvent.Type == Events.CheckoutSessionAsyncPaymentSucceeded)
            {
                 if (stripeEvent.Data.Object is Session session &&
                    session.Metadata != null &&
                    session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                     // Lógica similar para pagamentos assíncronos (ex: boletos) que compensaram depois
                     if (Guid.TryParse(orderIdString, out var orderId))
                    {
                        var transactionId = session.PaymentIntentId;
                        var amountPaid = session.AmountTotal ?? 0;
                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                    }
                }
            }
            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
            {
                _logger.LogWarning("[Webhook] Pagamento falhou: {EventId}", stripeEvent.Id);
            }
            else
            {
                // Logar eventos não tratados para fins de debug (opcional)
                _logger.LogInformation("[Webhook] Evento não tratado recebido: {EventType}", stripeEvent.Type);
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Erro validação Stripe.");
            return BadRequest();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro interno Webhook.");
            return StatusCode(500);
        }
    }

    private bool IsRequestFromStripe(IPAddress? remoteIp)
    {
        if (remoteIp == null) return false;

        if (IPAddress.IsLoopback(remoteIp)) return true;

        if (_authorizedIps.Count == 0) return false;

        var ip = remoteIp.MapToIPv4().ToString();
        return _authorizedIps.Contains(ip);
    }
}