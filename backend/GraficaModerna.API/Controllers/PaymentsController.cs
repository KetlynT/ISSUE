using System.Security.Claims;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("StrictPaymentPolicy")]
public class PaymentsController(
    IPaymentService paymentService,
    IContentService contentService,
    AppDbContext context,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private readonly IContentService _contentService = contentService;

    private async Task CheckPurchaseEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("Pagamentos estão desativados temporariamente.");
    }

    [HttpPost("checkout-session/{orderId}")]
    public async Task<IActionResult> CreateSession(Guid orderId)
    {
        try
        {
            await CheckPurchaseEnabled();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Tentativa de criar sessão sem userId válido");
            return Unauthorized("Usuário não identificado.");
        }

        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            logger.LogWarning("Tentativa de acesso não autorizado ou pedido inexistente.");
            return NotFound("Pedido não encontrado ou você não tem permissão para acessá-lo.");
        }

        if (order.Status == "Pago") return BadRequest(new { message = "Este pedido já está pago." });

        if (order.Status == "Cancelado" || order.Status == "Reembolsado")
            return BadRequest(new { message = "Este pedido foi cancelado e não pode ser pago." });

        if (order.Items.Count == 0)
        {
            logger.LogError("Pedido {OrderId} sem itens tentando criar sessão de pagamento", orderId);
            return BadRequest(new { message = "Pedido inválido: sem itens." });
        }

        if (order.TotalAmount <= 0)
        {
            logger.LogError("Pedido {OrderId} com valor inválido", orderId);
            return BadRequest(new { message = "Pedido com valor inválido." });
        }

        try
        {
            var url = await paymentService.CreateCheckoutSessionAsync(order);

            logger.LogInformation(
                "Sessão de pagamento criada com sucesso. OrderId: {OrderId}",
                orderId);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            logger.LogError(
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

        var order = await context.Orders
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound("Pedido não encontrado.");

        return Ok(order);
    }
}