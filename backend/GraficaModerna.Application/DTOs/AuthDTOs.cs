using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record LoginDto(
    [Required] string Email,
    [Required] string Password
);

public record RegisterDto(
    [Required] string FullName,
    [Required] [EmailAddress] string Email,
    [Required] string CpfCnpj,
    [Required] string Password,
    string PhoneNumber
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Role,
    string CpfCnpj
);

public class TokenModel
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
