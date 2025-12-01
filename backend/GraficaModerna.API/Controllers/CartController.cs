using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart() => Ok(await _cartService.GetCartAsync(GetUserId()));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartDto dto)
    {
        try { await _cartService.AddItemAsync(GetUserId(), dto); return Ok(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(Guid itemId)
    {
        await _cartService.RemoveItemAsync(GetUserId(), itemId);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(GetUserId());
        return Ok();
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            var order = await _cartService.CheckoutAsync(GetUserId(), request.Address, request.ZipCode, request.CouponCode);
            return Ok(order);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}

public record CheckoutRequest(string Address, string ZipCode, string? CouponCode);