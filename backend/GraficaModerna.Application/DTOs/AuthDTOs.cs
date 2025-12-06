using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record LoginDto(
    [Required] string Email,
    [Required] string Password
);

public record RegisterDto(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string PhoneNumber
);

// Atualizado para incluir RefreshToken
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Role
);

// Novo DTO para o endpoint de Refresh
public class TokenModel
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

public record UserProfileDto(string FullName, string Email, string PhoneNumber);
public record UpdateProfileDto(string FullName, string PhoneNumber);