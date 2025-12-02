using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) { _orderService = orderService; }

    [HttpGet]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _orderService.GetUserOrdersAsync(userId!));
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<OrderDto>>> GetAllOrders() => Ok(await _orderService.GetAllOrdersAsync());

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            await _orderService.UpdateOrderStatusAsync(id, dto.Status, dto.TrackingCode);
            return NoContent();
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    // NOVO ENDPOINT: Solicitar Reembolso (Cliente)
    [HttpPost("{id}/request-refund")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> RequestRefund(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _orderService.RequestRefundAsync(id, userId!);
            return Ok(new { message = "Reembolso solicitado com sucesso." });
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}