using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class PaymentController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration; // Injetar Configuração

    public PaymentController(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    [HttpPost("pay/{orderId}")]
    public async Task<IActionResult> SimulatePayment(Guid orderId)
    {
        // --- TRAVA DE SEGURANÇA (FEATURE FLAG) ---
        // Se a configuração não existir ou for false, bloqueia.
        var isDevMode = _configuration.GetValue<bool>("PaymentSettings:EnableDevPayment");

        if (!isDevMode)
        {
            return StatusCode(403, new { message = "Pagamento manual desabilitado. Utilize o gateway de pagamento." });
        }
        // ------------------------------------------

        await Task.Delay(1000);
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuário não identificado.");

            await _orderService.PayOrderAsync(orderId, userId);

            return Ok(new { message = "Aprovado (Modo Simulação)!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}