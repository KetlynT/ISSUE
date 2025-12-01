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
    private readonly ICartService _cartService;

    public OrdersController(ICartService cartService) { _cartService = cartService; }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _cartService.GetUserOrdersAsync(userId!));
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<OrderDto>>> GetAllOrders() => Ok(await _cartService.GetAllOrdersAsync());

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string newStatus)
    {
        try { await _cartService.UpdateOrderStatusAsync(id, newStatus); return NoContent(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}