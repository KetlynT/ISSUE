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

    // O segredo deve vir de Variável de Ambiente ou User Secrets (NUNCA hardcoded)
    private string EndpointSecret => _configuration["Stripe:WebhookSecret"]
        ?? throw new InvalidOperationException("Segredo do Webhook Stripe não configurado.");

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

        try
        {
            // 1. SEGURANÇA MÁXIMA: Valida a assinatura do Stripe
            // Isso garante que o JSON não foi forjado por um atacante
            var signature = Request.Headers["Stripe-Signature"];

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                EndpointSecret
            );

            // 2. Processa apenas eventos relevantes
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.Metadata.ContainsKey("order_id"))
                {
                    var orderIdString = session.Metadata["order_id"];
                    var transactionId = session.PaymentIntentId; // ID da transação no Stripe

                    if (Guid.TryParse(orderIdString, out Guid orderId))
                    {
                        _logger.LogInformation($"Pagamento confirmado pelo Stripe para o Pedido {orderId}. Transação: {transactionId}");

                        // 3. Atualiza o pedido (Idempotência garantida pelo OrderService)
                        await _orderService.ConfirmPaymentViaWebhookAsync(orderId, transactionId);
                    }
                    else
                    {
                        _logger.LogError($"Metadata 'order_id' inválido no evento Stripe: {orderIdString}");
                    }
                }
            }
            else
            {
                // Outros eventos (ex: pagamento falhou) podem ser tratados aqui
                _logger.LogInformation($"Evento Stripe não tratado: {stripeEvent.Type}");
            }

            // Retorna 200 OK rapidamente para o Stripe não reenviar o evento
            return Ok();
        }
        catch (StripeException e)
        {
            // Erro de assinatura ou formato inválido (Provável ataque ou config errada)
            _logger.LogError(e, "Erro ao validar Webhook Stripe.");
            return BadRequest();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Erro interno ao processar Webhook.");
            return StatusCode(500);
        }
    }
}