using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CartController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
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

    [HttpPatch("items/{itemId}")]
    public async Task<IActionResult> UpdateQuantity(Guid itemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            await _cartService.UpdateItemQuantityAsync(GetUserId(), itemId, dto.Quantity);
            return Ok();
        }
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
            if (string.IsNullOrEmpty(request.Address.ZipCode) || string.IsNullOrEmpty(request.Address.Street))
                return BadRequest("Endereço de entrega inválido.");

            // CORREÇÃO: Passa o frete para o serviço
            var order = await _orderService.CreateOrderFromCartAsync(
                GetUserId(),
                request.Address,
                request.CouponCode,
                request.ShippingCost,
                request.ShippingMethod
            );

            return Ok(order);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}

// Atualizamos o DTO para incluir o frete
public record CheckoutRequest(CreateAddressDto Address, string? CouponCode, decimal ShippingCost, string ShippingMethod);