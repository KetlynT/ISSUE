using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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

        // CORREÇÃO CRÍTICA: Validar ownership do pedido
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            _logger.LogWarning(
                "Tentativa de acesso não autorizado ao pedido {OrderId} pelo usuário {UserId}",
                orderId, userId);
            return NotFound("Pedido não encontrado ou você não tem permissão para acessá-lo.");
        }

        // CORREÇÃO: Validar status do pedido
        if (order.Status == "Pago")
        {
            return BadRequest(new { message = "Este pedido já está pago." });
        }

        if (order.Status == "Cancelado" || order.Status == "Reembolsado")
        {
            return BadRequest(new { message = "Este pedido foi cancelado e não pode ser pago." });
        }

        // CORREÇÃO: Validar que o pedido tem itens e valor válido
        if (!order.Items.Any())
        {
            _logger.LogError("Pedido {OrderId} sem itens tentando criar sessão de pagamento", orderId);
            return BadRequest(new { message = "Pedido inválido: sem itens." });
        }

        if (order.TotalAmount <= 0)
        {
            _logger.LogError("Pedido {OrderId} com valor inválido: {Total}", orderId, order.TotalAmount);
            return BadRequest(new { message = "Pedido com valor inválido." });
        }

        try
        {
            var url = await _paymentService.CreateCheckoutSessionAsync(order);

            _logger.LogInformation(
                "Sessão de pagamento criada. Pedido: {OrderId}, Usuário: {UserId}",
                orderId, userId);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao criar sessão de pagamento. Pedido: {OrderId}, Usuário: {UserId}",
                orderId, userId);

            // Retorna mensagem genérica para evitar exposição de detalhes internos
            return StatusCode(500, new
            {
                message = "Erro ao processar pagamento. Tente novamente em alguns instantes."
            });
        }
    }

    // NOVO: Endpoint para verificar status do pagamento
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _context.Orders
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.StripeSessionId,
                o.StripePaymentIntentId
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound("Pedido não encontrado.");
        }

        return Ok(order);
    }
}