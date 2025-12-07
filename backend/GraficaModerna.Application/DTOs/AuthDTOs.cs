using System.ComponentModel.DataAnnotations;

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
        var cleanDoc = CpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

        if (cleanDoc.Length != 11 && cleanDoc.Length != 14)
        {
            yield return new ValidationResult("O documento deve ter 11 (CPF) ou 14 (CNPJ) dígitos.", [nameof(CpfCnpj)]);
        }
        else if (cleanDoc.Length == 11)
        {
            if (!IsCpfValid(cleanDoc))
                yield return new ValidationResult("CPF inválido.", [nameof(CpfCnpj)]);
        }
        else
        {
            if (!IsCnpjValid(cleanDoc))
                yield return new ValidationResult("CNPJ inválido.", [nameof(CpfCnpj)]);
        }
    }

    private static bool IsCpfValid(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        var multiplier1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf[..9];
        var sum = 0;

        for (var i = 0; i < 9; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier1[i];

        var remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        var digit = remainder.ToString();
        tempCpf += digit;
        sum = 0;

        for (var i = 0; i < 10; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        digit += remainder.ToString();

        return cpf.EndsWith(digit);
    }

    private static bool IsCnpjValid(string cnpj)
    {
        var multiplier1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCnpj = cnpj[..12];
        var sum = 0;

        for (var i = 0; i < 12; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

        var remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        var digit = remainder.ToString();
        tempCnpj += digit;
        sum = 0;

        for (var i = 0; i < 13; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        digit += remainder.ToString();

        return cnpj.EndsWith(digit);
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
