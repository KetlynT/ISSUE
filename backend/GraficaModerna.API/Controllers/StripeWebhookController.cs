using System.Net;
using System.Security;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.API.Controllers;

[Route("api/webhook")]
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("WebhookPolicy")]
public class StripeWebhookController(
    IConfiguration configuration,
    IOrderService orderService,
    ILogger<StripeWebhookController> logger,
    MetadataSecurityService securityService,
    AppDbContext context) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<StripeWebhookController> _logger = logger;
    private readonly IOrderService _orderService = orderService;
    private readonly MetadataSecurityService _securityService = securityService;
    private readonly AppDbContext _context = context;

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeEvent()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")!;

        try
        {
            var signature = Request.Headers["Stripe-Signature"];
            var stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret);

            var eventExists = await _context.ProcessedWebhookEvents.AnyAsync(e => e.EventId == stripeEvent.Id);
            if (eventExists)
            {
                _logger.LogInformation("Evento {EventId} já processado anteriormente. Ignorando.", stripeEvent.Id);
                return Ok();
            }

            _context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent { EventId = stripeEvent.Id });
            await _context.SaveChangesAsync();

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
                                _logger.LogError("ID descriptografado inválido no evento {EventId}.", stripeEvent.Id);
                                return BadRequest("Invalid Order ID format");
                            }

                            var transactionId = session.PaymentIntentId;
                            var amountPaid = session.AmountTotal ?? 0;

                            if (string.IsNullOrEmpty(transactionId))
                            {
                                _logger.LogError("[Webhook] PaymentIntentId ausente para Order {OrderId}. Event: {EventId}", orderId, stripeEvent.Id);
                                return BadRequest("Missing PaymentIntentId");
                            }

                            if (amountPaid <= 0)
                            {
                                _logger.LogError("[Webhook] Valor pago inválido ({Amount}) para Order {OrderId}. Event: {EventId}",
                                    amountPaid, orderId, stripeEvent.Id);
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
                            _logger.LogCritical("FALHA DE INTEGRIDADE: Assinatura dos metadados inválida no Webhook {EventId}.", stripeEvent.Id);
                            return StatusCode(403, "Integrity Check Failed");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex,
                                "[Webhook] ERRO CRÍTICO ao confirmar pagamento. Event: {EventId}", stripeEvent.Id);
                            return StatusCode(500, "Payment confirmation failed");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[Webhook] Metadados de segurança ausentes ou inválidos no evento {EventId}", stripeEvent.Id);
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
                                    "[Webhook] Pagamento assíncrono bem-sucedido para Order {OrderId}. Event: {EventId}", orderId, stripeEvent.Id);

                                await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId, amountPaid);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Webhook] Erro ao processar metadados no pagamento assíncrono. Event: {EventId}", stripeEvent.Id);
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
                _logger.LogInformation("[Webhook] Evento não tratado recebido: {EventType}. ID: {EventId}", stripeEvent.Type, stripeEvent.Id);
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