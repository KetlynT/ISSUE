using System.ComponentModel.DataAnnotations;
using GraficaModerna.Application.Validators;

namespace GraficaModerna.Application.DTOs;

public record LoginDto(
    [Required] string Email,
    [Required] string Password,
    bool IsAdminLogin = false
);

public record RegisterDto(
    [Required] [EmailAddress] string Email,
    [Required] string Password,
    [Required] string FullName,
    [Required] string CpfCnpj,
    [Required] string PhoneNumber
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DocumentValidator.IsValid(CpfCnpj))
        {
            yield return new ValidationResult("Documento (CPF ou CNPJ) inválido.", [nameof(CpfCnpj)]);
        }
    }
}

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