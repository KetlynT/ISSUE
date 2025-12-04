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
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        SetTokenCookie(result.Token);
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
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Deslogado com sucesso" });
    }

    [HttpGet("profile")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // O serviço lança exceção se o usuário não for encontrado (ex: banco resetado)
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception)
        {
            // SEGURANÇA: Se der erro ao buscar perfil, assume token inválido/usuário deletado
            // Força limpeza do cookie e retorna 401 para o front redirecionar
            Response.Cookies.Delete("jwt");
            return Unauthorized();
        }
    }

    [HttpPut("profile")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _authService.UpdateProfileAsync(userId, dto);
            return NoContent();
        }
        catch
        {
            return Unauthorized();
        }
    }

    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(8)
        };
        Response.Cookies.Append("jwt", token, cookieOptions);
    }
}