using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ITokenBlacklistService blacklistService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _blacklistService = blacklistService;
        _logger = logger;
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        // Return token in body so SPA can store it (Authorization: Bearer)
        return Ok(new { token = result.AccessToken, result.Email, result.Role, message = "Cadastro realizado com sucesso." });
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        // Return token in body so SPA can store it (Authorization: Bearer)
        return Ok(new { token = result.AccessToken, result.Email, result.Role, message = "Login realizado com sucesso." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Apenas Header é aceito
        string? token = null;

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var header = authHeader.ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = header.Substring("Bearer ".Length).Trim();
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                // Blacklist é a forma correta de invalidar JWT antes da expiração
                await _blacklistService.BlacklistTokenAsync(token, jwtToken.ValidTo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro ao processar blacklist no logout: {Message}", ex.Message);
            }
        }

        return Ok(new { message = "Deslogado com sucesso" });
    }

    [HttpGet("check-auth")]
    [Authorize]
    public IActionResult CheckAuth()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { isAuthenticated = true, role });
    }

    [HttpGet("profile")]
    [Authorize(Roles = "User,Admin")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var profile = await _authService.GetProfileAsync(userId);
        return Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _authService.UpdateProfileAsync(userId, dto);
        return NoContent();
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous] // Importante: O usuário não tem token válido quando chama isso
    public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
    {
        if (tokenModel is null)
        {
            return BadRequest("Requisição inválida");
        }

        try
        {
            var result = await _authService.RefreshTokenAsync(tokenModel);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}