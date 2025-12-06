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

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

            // SEGURAN?A: N?o passamos mais o ShippingCost vindo do cliente
            var order = await _orderService.CreateOrderFromCartAsync(
                userId,
                dto.Address,
                dto.CouponCode,
                dto.ShippingMethod
            );

            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> RequestCancel(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

            await _orderService.RequestRefundAsync(id, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CheckoutDto
{
    public required CreateAddressDto Address { get; set; }
    public string? CouponCode { get; set; }
    public required string ShippingMethod { get; set; }
}