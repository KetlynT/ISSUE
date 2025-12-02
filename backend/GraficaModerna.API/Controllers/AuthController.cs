using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        SetTokenCookie(result.Token);
        // Retorna apenas dados não sensíveis
        return Ok(new { result.Email, result.Role });
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        SetTokenCookie(result.Token);
        return Ok(new { result.Email, result.Role });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Remove o cookie do navegador
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Deslogado com sucesso" });
    }

    [HttpGet("profile")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _authService.GetProfileAsync(userId!));
    }

    [HttpPut("profile")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _authService.UpdateProfileAsync(userId!, dto);
        return NoContent();
    }

    // --- MÁGICA DE SEGURANÇA: Cria o Cookie HttpOnly ---
    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // JavaScript não consegue ler (Anti-XSS)
            Secure = true,   // Só trafega em HTTPS (Anti-Sniffing)
            SameSite = SameSiteMode.Strict, // Previne CSRF
            Expires = DateTime.UtcNow.AddHours(8)
        };
        Response.Cookies.Append("jwt", token, cookieOptions);
    }
}