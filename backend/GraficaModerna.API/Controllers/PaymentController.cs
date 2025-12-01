using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly ICartService _cartService;

    public PaymentController(ICartService cartService) { _cartService = cartService; }

    [HttpPost("pay/{orderId}")]
    public async Task<IActionResult> SimulatePayment(Guid orderId)
    {
        await Task.Delay(1000); // Delay fake
        try { await _cartService.UpdateOrderStatusAsync(orderId, "Pago"); return Ok(new { message = "Aprovado!" }); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}