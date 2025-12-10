using System.Security.Claims;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController(IOrderService orderService, IContentService contentService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly IContentService _contentService = contentService;

    private async Task CheckPurchaseEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("Novos pedidos estão desativados temporariamente. Entre em contato para orçamento.");
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        try
        {
            await CheckPurchaseEnabled();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

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
    // Método atualizado para suportar paginação
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

        // Agora o controller reconhece o retorno PagedResultDto graças ao using
        var pagedOrders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        return Ok(pagedOrders);
    }

    [HttpPost("{id}/request-refund")]
    public async Task<IActionResult> RequestRefund(Guid id, [FromBody] RequestRefundDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "Usuário não autenticado." });

            await _orderService.RequestRefundAsync(id, userId, dto);
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