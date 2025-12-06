using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
// CORREÇÃO IDE0290: Parâmetros injetados direto na classe
public class CartController(ICartService cartService, IOrderService orderService) : ControllerBase
{
    // Atribuímos os parâmetros do construtor primário aos campos privados
    private readonly ICartService _cartService = cartService;
    private readonly IOrderService _orderService = orderService;

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

            var order = await _orderService.CreateOrderFromCartAsync(
                GetUserId(),
                request.Address,
                request.CouponCode,
                request.ShippingMethod
            );

            return Ok(order);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}

public record CheckoutRequest(CreateAddressDto Address, string? CouponCode, decimal ShippingCost, string ShippingMethod);