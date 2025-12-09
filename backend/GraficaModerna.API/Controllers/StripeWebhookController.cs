using System.Net;
using System.Security;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Security;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly IOrderService _orderService;
    private readonly MetadataSecurityService _securityService;

    public StripeWebhookController(
        IConfiguration configuration,
        IOrderService orderService,
        ILogger<StripeWebhookController> logger,
        MetadataSecurityService securityService)
    {
        _configuration = configuration;
        _orderService = orderService;
        _logger = logger;
        _securityService = securityService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeEvent()
    {
        // REMOVIDO: Validação por IP. O Stripe recomenda validar apenas a assinatura (Stripe-Signature).
        // IPs podem mudar sem aviso prévio, causando falsos positivos.

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
                if (stripeEvent.Data.Object is Session session && session.Metadata != null)
                {
                    if (session.Metadata.TryGetValue("order_data", out var encryptedOrder))
                    {
                        try
                        {
                            var plainOrderId = _securityService.Unprotect(encryptedOrder);

                            if (!Guid.TryParse(plainOrderId, out var orderId))
                            {
                                _logger.LogError("ID descriptografado inválido.");
                                return BadRequest("Invalid Order ID format");
                            }

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
                            }
                            catch (Exception ex) when (ex.Message.Contains("FATAL"))
                            {
                                _logger.LogCritical(ex, 
                                    "[SECURITY ALERT] Tentativa de fraude detectada. Order: {OrderId}, Transaction: {TransactionId}", 
                                    orderId, transactionId);

                                return BadRequest("Payment validation failed - security violation");
                            }

                            _logger.LogInformation(
                                "[Webhook] Pagamento confirmado com sucesso. Order {OrderId}", orderId);
                        }
                        catch (SecurityException)
                        {
                            _logger.LogCritical("FALHA DE INTEGRIDADE: Assinatura dos metadados inválida no Webhook.");
                            return StatusCode(403, "Integrity Check Failed");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex,
                                "[Webhook] ERRO CRÍTICO ao confirmar pagamento.");
                            return StatusCode(500, "Payment confirmation failed");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[Webhook] Metadados de segurança ausentes ou inválidos no evento checkout.session.completed");
                    }
                }
            }
            else if (stripeEvent.Type == Events.CheckoutSessionAsyncPaymentSucceeded)
            {
                if (stripeEvent.Data.Object is Session session && session.Metadata != null)
                {
                    if (session.Metadata.TryGetValue("order_data", out var encryptedOrder))
                    {
                        try
                        {
                            var plainOrderId = _securityService.Unprotect(encryptedOrder);
                            if (Guid.TryParse(plainOrderId, out var orderId))
                            {
                                var transactionId = session.PaymentIntentId;
                                var amountPaid = session.AmountTotal ?? 0;

                                _logger.LogInformation(
                                    "[Webhook] Pagamento assíncrono bem-sucedido para Order {OrderId}", orderId);

                                await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Webhook] Erro ao processar metadados no pagamento assíncrono.");
                        }
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
}