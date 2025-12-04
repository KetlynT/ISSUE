using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Apenas usuários logados podem iniciar pagamentos
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;

    public PaymentsController(
        IPaymentService paymentService,
        IOrderRepository orderRepository)
    {
        _paymentService = paymentService;
        _orderRepository = orderRepository;
    }

    [HttpPost("checkout/{orderId}")]
    public async Task<IActionResult> CreateCheckoutSession(Guid orderId)
    {
        try
        {
            // 1. Segurança: Garante que o pedido pertence ao usuário logado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
                return NotFound("Pedido não encontrado.");

            if (order.UserId != userId)
                return Forbid("Você não tem permissão para pagar este pedido.");

            if (order.Status != "Pendente")
                return BadRequest($"O status do pedido é {order.Status} e não pode ser pago novamente.");

            // 2. Cria a sessão no Stripe usando a camada de infraestrutura
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(order);

            return Ok(new { url = checkoutUrl });
        }
        catch (Exception ex)
        {
            // Logar o erro real no servidor e retornar mensagem genérica (opcionalmente logar 'ex')
            Console.WriteLine($"Erro no pagamento: {ex.Message}");
            return StatusCode(500, new { error = "Erro ao iniciar pagamento com Stripe. Tente novamente." });
        }
    }
}