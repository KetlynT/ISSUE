using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Application.Validators;
using GraficaModerna.Domain.Constants;
using GraficaModerna.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GraficaModerna.Application.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IContentService contentService,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IEmailService emailService) : IAuthService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher = passwordHasher;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IContentService _contentService = contentService;
    private readonly IEmailService _emailService = emailService;

    private async Task CheckPurchaseEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("O sistema está em modo orçamento. Login de clientes desativado.");
    }

    private async Task CheckRegistrationEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("O cadastro de novos clientes está temporariamente suspenso.");
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        await CheckRegistrationEnabled();

        if (!DocumentValidator.IsValid(dto.CpfCnpj))
        {
            throw new Exception("O documento informado (CPF ou CNPJ) é inválido.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            CpfCnpj = dto.CpfCnpj,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = false
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

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var frontendUrl = _configuration["FRONTEND_URL"] ?? "http://localhost:5173";
            var link = $"{frontendUrl}/confirm-email?userid={user.Id}&token={encodedToken}";

            var body = $@"
                <h2>Bem-vindo à Gráfica Moderna!</h2>
                <p>Olá {user.FullName}, obrigado por se cadastrar.</p>
                <p>Confirme seu e-mail clicando abaixo:</p>
                <p><a href='{link}'>CONFIRMAR CONTA</a></p>";

            await _emailService.SendEmailAsync(user.Email!, "Confirme sua conta", body);
        }
        catch
        {
        }

        return await CreateTokenPairAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
                   ?? throw new Exception("Credenciais inválidas.");

        if (await _userManager.IsLockedOutAsync(user))
        {
            var end = await _userManager.GetLockoutEndDateAsync(user);
            var timeLeft = end!.Value - DateTimeOffset.UtcNow;
            throw new Exception($"Conta bloqueada temporariamente. Tente novamente em {Math.Ceiling(timeLeft.TotalMinutes)} minutos.");
        }

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
                throw new Exception("Muitas tentativas falhas. Conta bloqueada temporariamente.");

            throw new Exception("Credenciais inválidas.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(Roles.Admin);

        if (dto.IsAdminLogin)
        {
            if (!isAdmin)
                throw new Exception("Acesso não autorizado para contas de cliente.");
        }
        else
        {
            if (isAdmin)
                throw new Exception("Administradores devem acessar exclusivamente pelo Painel Administrativo.");
        }

        if (!isAdmin)
        {
            await CheckPurchaseEnabled();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Alerta de Login",
                    $"<p>Novo login detectado em sua conta em {DateTime.Now:dd/MM/yyyy HH:mm}.</p>");
            }
            catch { }
        });

        return await CreateTokenPairAsync(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(TokenModel tokenModel)
    {
        if (tokenModel is null) throw new Exception("Requisição inválida");

        var accessToken = tokenModel.AccessToken;
        var refreshToken = tokenModel.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken)) throw new Exception("Refresh token inválido");

        var principal = GetPrincipalFromExpiredToken(accessToken) ??
                        throw new Exception("Token de acesso ou refresh token inválido");
        var username = principal.Identity!.Name!;

        var user = await _userManager.FindByNameAsync(username);

        if (user == null || user.RefreshToken == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new Exception("Refresh token inválido ou expirado.");

        if (await _userManager.IsLockedOutAsync(user))
            throw new Exception("Conta bloqueada temporariamente.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.RefreshToken, refreshToken);

        if (verificationResult != PasswordVerificationResult.Success)
            throw new Exception("Refresh token inválido ou expirado.");

        return await CreateTokenPairAsync(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception("Usuário não encontrado.");
        return new UserProfileDto(user.FullName, user.Email!, user.CpfCnpj, user.PhoneNumber ?? "");
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        if (!DocumentValidator.IsValid(dto.CpfCnpj))
        {
            throw new Exception("O documento informado (CPF ou CNPJ) é inválido.");
        }

        var user = await _userManager.FindByIdAsync(userId) ?? throw new Exception("Usuário não encontrado.");
        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.CpfCnpj = dto.CpfCnpj;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new Exception("Erro ao atualizar perfil.");
    }

    public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId) ?? throw new Exception("Usuário inválido.");

        var result = await _userManager.ConfirmEmailAsync(user, dto.Token);
        if (!result.Succeeded) throw new Exception("Falha ao confirmar e-mail.");
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return;

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var frontendUrl = _configuration["FRONTEND_URL"] ?? "http://localhost:5173";
            var link = $"{frontendUrl}/reset-password?email={dto.Email}&token={encodedToken}";

            var body = $@"
                <h2>Recuperação de Senha</h2>
                <p>Clique abaixo para redefinir sua senha:</p>
                <p><a href='{link}'>REDEFINIR SENHA</a></p>";

            await _emailService.SendEmailAsync(user.Email!, "Recuperação de Senha", body);
        }
        catch { }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
                   ?? throw new Exception("Usuário não encontrado.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

        if (!result.Succeeded)
            throw new Exception("Erro ao redefinir senha: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        _ = Task.Run(() => _emailService.SendEmailAsync(user.Email!, "Senha Alterada", "<p>Sua senha foi alterada com sucesso.</p>"));
    }

    private async Task<AuthResponseDto> CreateTokenPairAsync(ApplicationUser user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = _passwordHasher.HashPassword(user, refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : roles.FirstOrDefault() ?? Roles.User;

        return new AuthResponseDto(new JwtSecurityTokenHandler().WriteToken(accessToken), refreshToken, user.Email!,
            primaryRole, user.CpfCnpj);
    }

    private JwtSecurityToken GenerateAccessToken(ApplicationUser user)
    {
        var userRoles = _userManager.GetRolesAsync(user).Result;
        var primaryRole = userRoles.Contains(Roles.Admin) ? Roles.Admin : userRoles.FirstOrDefault() ?? Roles.User;

        var authClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", primaryRole),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!)
        };

        var keyString = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyString) || keyString.Length < 32) throw new Exception("Erro config JWT");
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

        return new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(15),
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
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token inválido");

            return principal;
        }
        catch
        {
            return null;
        }
    }
}