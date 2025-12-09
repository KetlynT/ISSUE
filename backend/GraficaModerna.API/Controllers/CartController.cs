using System.Security.Claims;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class CartController(ICartService cartService, IOrderService orderService, IContentService contentService) : ControllerBase
{
    private readonly ICartService _cartService = cartService;
    private readonly IOrderService _orderService = orderService;
    private readonly IContentService _contentService = contentService;

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private async Task CheckPurchaseEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("Funcionalidade de compra indisponível temporariamente.");
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        try 
        {
            await CheckPurchaseEnabled();
            return Ok(await _cartService.GetCartAsync(GetUserId()));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartDto dto)
    {
        try
        {
            await CheckPurchaseEnabled();
            await _cartService.AddItemAsync(GetUserId(), dto);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("items/{itemId}")]
    public async Task<IActionResult> UpdateQuantity(Guid itemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            await CheckPurchaseEnabled();
            await _cartService.UpdateItemQuantityAsync(GetUserId(), itemId, dto.Quantity);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(Guid itemId)
    {
        try
        {
            await CheckPurchaseEnabled();
            await _cartService.RemoveItemAsync(GetUserId(), itemId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            await CheckPurchaseEnabled();
            await _cartService.ClearCartAsync(GetUserId());
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record CheckoutRequest(
    CreateAddressDto Address,
    string? CouponCode,
    decimal ShippingCost,
    string ShippingMethod);