using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _service;

    public CouponsController(ICouponService service) { _service = service; }

    [HttpGet("validate/{code}")]
    public async Task<IActionResult> Validate(string code)
    {
        var coupon = await _service.GetValidCouponAsync(code);
        if (coupon == null) return NotFound("Inválido.");
        return Ok(new { coupon.Code, coupon.DiscountPercentage });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CouponResponseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CouponResponseDto>> Create(CreateCouponDto dto)
    {
        try { return Ok(await _service.CreateAsync(dto)); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}