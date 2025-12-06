using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// SEGURANÇA MÁXIMA: Apenas Tokens com a Claim "Role: Admin" entram aqui.
[Authorize(Roles = "Admin")]
[EnableRateLimiting("AdminPolicy")]
public class AdminController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;

    public AdminController(IOrderService orderService, IProductService productService)
    {
        _orderService = orderService;
        _productService = productService;
    }

    // Endpoint para Listar Todos os Pedidos
    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    // Endpoint CRÍTICO: Atualizar Status e Processar Estorno
    // Como está protegido por [Authorize(Roles = "Admin")], hackers não conseguem acessá-lo.
    [HttpPut("orders/{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            // Chama a lógica segura que faz o reembolso no Stripe se necessário
            await _orderService.UpdateAdminOrderAsync(id, dto);
            return Ok(new { message = "Status atualizado com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}