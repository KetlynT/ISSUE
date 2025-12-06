using System.Security.Claims;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("PaymentPolicy")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        AppDbContext context,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("checkout-session/{orderId}")]
    public async Task<IActionResult> CreateSession(Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Tentativa de criar sessão sem userId válido");
            return Unauthorized("Usuário não identificado.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            // SEGURANÇA: Logamos o evento de segurança, mas evitamos logar detalhes sensíveis do objeto order se ele existisse mas fosse de outro user
            _logger.LogWarning(
                "Tentativa de acesso não autorizado ou pedido inexistente. OrderId: {OrderId}, UserId: {UserId}",
                orderId, userId);
            return NotFound("Pedido não encontrado ou você não tem permissão para acessá-lo.");
        }

        if (order.Status == "Pago")
        {
            return BadRequest(new { message = "Este pedido já está pago." });
        }

        if (order.Status == "Cancelado" || order.Status == "Reembolsado")
        {
            return BadRequest(new { message = "Este pedido foi cancelado e não pode ser pago." });
        }

        if (!order.Items.Any())
        {
            _logger.LogError("Pedido {OrderId} sem itens tentando criar sessão de pagamento", orderId);
            return BadRequest(new { message = "Pedido inválido: sem itens." });
        }

        if (order.TotalAmount <= 0)
        {
            _logger.LogError("Pedido {OrderId} com valor inválido", orderId);
            return BadRequest(new { message = "Pedido com valor inválido." });
        }

        try
        {
            var url = await _paymentService.CreateCheckoutSessionAsync(order);

            _logger.LogInformation(
                "Sessão de pagamento criada com sucesso. OrderId: {OrderId}",
                orderId);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            // SEGURANÇA: Logamos a exceção completa no servidor, mas retornamos apenas mensagem genérica
            _logger.LogError(
                ex,
                "Erro ao processar pagamento. OrderId: {OrderId}",
                orderId);

            return StatusCode(500, new
            {
                message = "Erro ao processar pagamento. Tente novamente em alguns instantes."
            });
        }
    }

    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // SEGURANÇA: Projeção (Select) explícita para evitar Over-Posting ou vazamento de dados internos
        var order = await _context.Orders
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount
                // REMOVIDO: StripeSessionId e StripePaymentIntentId para evitar exposição de detalhes de infraestrutura
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound("Pedido não encontrado.");
        }

        return Ok(order);
    }
}