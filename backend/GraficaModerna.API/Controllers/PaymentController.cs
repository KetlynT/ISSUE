using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")] // Apenas Clientes podem executar pagamentos
public class PaymentController : ControllerBase
{
    private readonly IOrderService _orderService;

    public PaymentController(IOrderService orderService) { _orderService = orderService; }

    [HttpPost("pay/{orderId}")]
    public async Task<IActionResult> SimulatePayment(Guid orderId)
    {
        await Task.Delay(1000); // Simula processamento
        try
        {
            // CORREÇÃO: Passando 'null' como terceiro argumento (trackingCode),
            // pois o pagamento não gera código de rastreio.
            await _orderService.UpdateOrderStatusAsync(orderId, "Pago", null);

            return Ok(new { message = "Aprovado!" });
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}