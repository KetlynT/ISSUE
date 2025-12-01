using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization; // Importante para o [Authorize]
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Importante para pegar o ID do token

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

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        return Ok(await _authService.RegisterAsync(dto));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        return Ok(await _authService.LoginAsync(dto));
    }

    // --- NOVAS ROTAS ---

    [HttpGet("profile")]
    [Authorize] // Requer login
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _authService.GetProfileAsync(userId!));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _authService.UpdateProfileAsync(userId!, dto);
        return NoContent();
    }
}