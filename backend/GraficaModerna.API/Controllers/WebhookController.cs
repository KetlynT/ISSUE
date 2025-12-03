using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IOrderService orderService, IConfiguration configuration, ILogger<WebhookController> logger)
    {
        _orderService = orderService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("payment-update")]
    [AllowAnonymous] // Gateways externos não têm login
    public async Task<IActionResult> ReceiveNotification()
    {
        // 1. Leitura do Corpo da Requisição (Payload)
        using var reader = new StreamReader(HttpContext.Request.Body);
        var jsonBody = await reader.ReadToEndAsync();

        // 2. SEGURANÇA: Validação da Assinatura (Simulado)
        // Todo gateway envia um Header (ex: 'X-Signature', 'Stripe-Signature')
        // Você deve pegar esse header e comparar com uma chave secreta no seu .env

        var signature = Request.Headers["X-Gateway-Signature"].FirstOrDefault();
        var webhookSecret = _configuration["PaymentSettings:WebhookSecret"]; // Vem do appsettings ou ENV

        // Lógica Falsa de validação (substitua pela biblioteca do gateway no futuro)
        bool isValidSignature = !string.IsNullOrEmpty(webhookSecret); // && Validate(jsonBody, signature, webhookSecret)

        if (!isValidSignature)
        {
            _logger.LogWarning("Tentativa de Webhook com assinatura inválida.");
            return Unauthorized();
        }

        try
        {
            // 3. Extração de dados (Isso muda dependendo do Gateway escolhido)
            // Supondo que o JSON venha: { "orderId": "...", "status": "approved", "txn_id": "..." }

            // MOCK (Simulação de extração):
            // Em produção, use JsonSerializer.Deserialize<GatewayDto>(jsonBody)
            var mockOrderId = Guid.Empty; // Substitua pela lógica real de extração
            var status = "approved";
            var transactionId = "txn_123456";

            if (status == "approved")
            {
                await _orderService.ConfirmPaymentViaWebhookAsync(mockOrderId, transactionId);
                _logger.LogInformation($"Pagamento aprovado via Webhook para o pedido {mockOrderId}");
            }

            // Sempre retorne 200 OK para o Gateway não ficar tentando reenviar
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook");
            // Retornar 500 faz o gateway tentar de novo mais tarde (bom para erros de banco)
            return StatusCode(500);
        }
    }
}