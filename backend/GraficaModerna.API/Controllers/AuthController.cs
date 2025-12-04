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
    private readonly ITokenBlacklistService _blacklistService; // Injeção da Blacklist
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

    [EnableRateLimiting("AuthPolicy")] // Proteção contra Brute Force
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        SetTokenCookie(result.Token);

        // SEGURANÇA: Não retornamos o token no corpo da resposta
        return Ok(new { result.Email, result.Role, message = "Cadastro realizado com sucesso." });
    }

    [EnableRateLimiting("AuthPolicy")] // Proteção contra Brute Force
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        SetTokenCookie(result.Token);

        // SEGURANÇA: Token vai apenas no Cookie, invisível ao JS
        return Ok(new { result.Email, result.Role, message = "Login realizado com sucesso." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Tenta pegar o token do cookie para colocar na blacklist
        if (Request.Cookies.TryGetValue("jwt", out var token))
        {
            try
            {
                // Lê o token para descobrir quando ele expira
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Adiciona à blacklist até a data de expiração original
                await _blacklistService.BlacklistTokenAsync(token, jwtToken.ValidTo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erro ao processar blacklist no logout: {ex.Message}");
                // Não falha a requisição, apenas segue para limpar o cookie
            }
        }

        // Remove o cookie do navegador
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Deslogado com sucesso" });
    }

    [HttpGet("check-auth")]
    [Authorize] // Verifica se o cookie é válido
    public IActionResult CheckAuth()
    {
        // Endpoint útil para o Frontend verificar se o usuário ainda está logado
        // sem precisar expor dados sensíveis.
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { isAuthenticated = true, role });
    }

    [HttpGet("profile")]
    [Authorize(Roles = "User,Admin")] // Aceita ambos
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

    // Método Privado para Configurar o Cookie Seguro
    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,   // JavaScript não acessa (Previne XSS)
            Secure = true,     // Apenas HTTPS (Em localhost o navegador tolera se tiver certificado dev)
            SameSite = SameSiteMode.Strict, // Previne CSRF
            Expires = DateTime.UtcNow.AddMinutes(30) // Sincronizado com o token
        };
        Response.Cookies.Append("jwt", token, cookieOptions);
    }
}