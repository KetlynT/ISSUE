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
            _logger.LogWarning("ALERTA: 'STRIPE_WEBHOOK_IPS' não configurado.");
        }
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeEvent()
    {
        if (!IsRequestFromStripe(HttpContext.Connection.RemoteIpAddress))
        {
            _logger.LogWarning("[Webhook] Tentativa de acesso bloqueada de IP não autorizado: {IP}", 
                HttpContext.Connection.RemoteIpAddress);
            return StatusCode(403, "Forbidden: Source Denied");
        }

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                             ?? _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(endpointSecret))
        {
            _logger.LogError("CRÍTICO: Stripe Webhook Secret não configurado.");
            return StatusCode(500);
        }

        try
        {
            var signature = Request.Headers["Stripe-Signature"];
            var stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret);

            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                if (stripeEvent.Data.Object is Session session &&
                    session.Metadata != null &&
                    session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out var orderId))
                    {
                        var transactionId = session.PaymentIntentId;
                        var amountPaid = session.AmountTotal ?? 0;

                        if (string.IsNullOrEmpty(transactionId))
                        {
                            _logger.LogError("[Webhook] PaymentIntentId ausente para Order {OrderId}", orderId);
                            return BadRequest("Missing PaymentIntentId");
                        }

                        if (amountPaid <= 0)
                        {
                            _logger.LogError("[Webhook] Valor pago inválido ({Amount}) para Order {OrderId}", 
                                amountPaid, orderId);
                            return BadRequest("Invalid payment amount");
                        }

                        _logger.LogInformation(
                            "[Webhook] Processando pagamento para Order {OrderId}. Transaction: {TransactionId}. Amount: {Amount} cents",
                            orderId, transactionId, amountPaid);

                        try
                        {
                            await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                            
                            _logger.LogInformation(
                                "[Webhook] Pagamento confirmado com sucesso. Order {OrderId}", orderId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, 
                                "[Webhook] ERRO CRÍTICO ao confirmar pagamento. Order {OrderId}, Transaction {TransactionId}", 
                                orderId, transactionId);
                            return StatusCode(500, "Payment confirmation failed");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[Webhook] Order ID inválido nos metadados: {OrderIdString}", orderIdString);
                    }
                }
                else
                {
                    _logger.LogWarning("[Webhook] Metadados ausentes ou inválidos no evento checkout.session.completed");
                }
            }
            else if (stripeEvent.Type == Events.CheckoutSessionAsyncPaymentSucceeded)
            {
                if (stripeEvent.Data.Object is Session session &&
                    session.Metadata != null &&
                    session.Metadata.TryGetValue("order_id", out var orderIdString))
                {
                    if (Guid.TryParse(orderIdString, out var orderId))
                    {
                        var transactionId = session.PaymentIntentId;
                        var amountPaid = session.AmountTotal ?? 0;

                        _logger.LogInformation(
                            "[Webhook] Pagamento assíncrono bem-sucedido para Order {OrderId}", orderId);

                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                    }
                }
            }
            else if (stripeEvent.Type == Events.CheckoutSessionAsyncPaymentFailed)
            {
                _logger.LogWarning("[Webhook] Pagamento assíncrono falhou: {EventId}", stripeEvent.Id);
            }
            else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
            {
                _logger.LogWarning("[Webhook] Pagamento falhou: {EventId}", stripeEvent.Id);
            }
            else
            {
                _logger.LogInformation("[Webhook] Evento não tratado recebido: {EventType}", stripeEvent.Type);
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "[Webhook] Erro de validação Stripe. Possível assinatura inválida.");
            return BadRequest("Invalid signature");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[Webhook] Erro interno ao processar webhook.");
            return StatusCode(500, "Internal server error");
        }
    }

    private bool IsRequestFromStripe(IPAddress? remoteIp)
    {
        if (remoteIp == null) 
        {
            _logger.LogWarning("[Webhook] IP remoto é nulo");
            return false;
        }

        if (IPAddress.IsLoopback(remoteIp)) 
            return true;

        if (_authorizedIps.Count == 0)
        {
            _logger.LogWarning("[Webhook] Lista de IPs autorizados está vazia. Bloqueando por segurança.");
            return false;
        }

        var ip = remoteIp.MapToIPv4().ToString();
        var isAuthorized = _authorizedIps.Contains(ip);

        if (!isAuthorized)
        {
            _logger.LogWarning("[Webhook] IP não autorizado tentando acessar: {IP}", ip);
        }

        return isAuthorized;
    }
}