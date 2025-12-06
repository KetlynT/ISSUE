using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Constants;
using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography; // Necessário para RNG
using System.Text;

namespace GraficaModerna.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var safeErrors = result.Errors
               .Where(e => e.Code.StartsWith("Password"))
               .Select(e => e.Description);

            if (safeErrors.Any())
                throw new Exception($"Senha fraca: {string.Join("; ", safeErrors)}");

            throw new Exception("Erro ao criar usuário.");
        }

        await _userManager.AddToRoleAsync(user, Roles.User);

        // Gera Access + Refresh Token
        return await CreateTokenPairAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new Exception("Credenciais inválidas.");

        return await CreateTokenPairAsync(user);
    }

    // =================================================================
    //  LÓGICA DE REFRESH TOKEN (ROTAÇÃO)
    // =================================================================
    public async Task<AuthResponseDto> RefreshTokenAsync(TokenModel tokenModel)
    {
        if (tokenModel is null) throw new Exception("Requisição inválida");

        string? accessToken = tokenModel.AccessToken;
        string? refreshToken = tokenModel.RefreshToken;

        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null) throw new Exception("Token de acesso ou refresh token inválido");

        string username = principal.Identity!.Name!; // Mapeado do ClaimTypes.Name (no Identity é o UserName/Email)

        // Como o Name no Identity pode ser UserName, buscamos pelo UserName
        var user = await _userManager.FindByNameAsync(username);

        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new Exception("Refresh token inválido ou expirado.");
        }

        // ROTAÇÃO: Geramos novos tokens e invalidamos o anterior
        return await CreateTokenPairAsync(user);
    }
    // =================================================================

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Usuário não encontrado.");
        return new UserProfileDto(user.FullName, user.Email!, user.PhoneNumber ?? "");
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Usuário não encontrado.");

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new Exception("Erro ao atualizar perfil.");
    }

    // --- MÉTODOS AUXILIARES ---

    private async Task<AuthResponseDto> CreateTokenPairAsync(ApplicationUser user)
    {
        // 1. Gera Access Token (Curta Duração: 15 min)
        var accessToken = GenerateAccessToken(user);

        // 2. Gera Refresh Token (Longa Duração: 7 dias)
        var refreshToken = GenerateRefreshToken();

        // 3. Salva Refresh Token no Banco
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Recupera Role para o retorno
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : (roles.FirstOrDefault() ?? Roles.User);

        return new AuthResponseDto(new JwtSecurityTokenHandler().WriteToken(accessToken), refreshToken, user.Email!, primaryRole);
    }

    private JwtSecurityToken GenerateAccessToken(ApplicationUser user)
    {
        var userRoles = _userManager.GetRolesAsync(user).Result;
        var primaryRole = userRoles.Contains(Roles.Admin) ? Roles.Admin : (userRoles.FirstOrDefault() ?? Roles.User);

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, primaryRole),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString) || keyString.Length < 32) throw new Exception("Erro config JWT");
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

        return new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(15), // Vida Curta
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // Pode ser true se configurado estritamente
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString!)),
            ValidateLifetime = false // Importante: aqui ignoramos a expiração para ler o token antigo
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token inválido");
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}