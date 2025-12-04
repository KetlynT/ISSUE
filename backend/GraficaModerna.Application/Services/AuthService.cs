using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Constants; // Lote 1
using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        // A senha agora será validada pelas regras fortes definidas no Program.cs
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            // Pega a primeira mensagem de erro para ser mais amigável, mas mantém log seguro
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Falha ao registrar: {errors}");
        }

        // Usa a constante Roles.User para evitar erros de digitação
        await _userManager.AddToRoleAsync(user, Roles.User);

        return await GenerateToken(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new Exception("Credenciais inválidas."); // Mensagem genérica para evitar User Enumeration

        return await GenerateToken(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Usuário não encontrado.");
        return new UserProfileDto(user.FullName, user.Email!, user.PhoneNumber);
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

    private async Task<AuthResponseDto> GenerateToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Prioriza Admin se o usuário tiver ambos, senão pega o primeiro ou User
        var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : (roles.FirstOrDefault() ?? Roles.User);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, primaryRole)
        };

        // Garante a leitura da mesma chave usada no Program.cs
        var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];
        // Fallback apenas para garantir que não quebre se a env var falhar (mas deve estar alinhado com Program.cs)
        if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
            keyString = "chave_temporaria_super_secreta_para_dev_apenas_999";

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            // SEGURANÇA: Token de curta duração (30 min). 
            // O Refresh Token seria o próximo passo ideal, mas o Cookie HttpOnly + Blacklist já mitigam muito.
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = creds,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponseDto(tokenHandler.WriteToken(token), user.Email!, primaryRole);
    }
}