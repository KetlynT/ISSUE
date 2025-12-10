using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GraficaModerna.Application.Validators;

public partial class StrictEmailAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var email = value as string;
        if (string.IsNullOrEmpty(email))
            return new ValidationResult("O e-mail é obrigatório.");

        try
        {
            var regex = EmailRegex();
            if (!regex.IsMatch(email))
                return new ValidationResult("Formato de e-mail inválido.");

            var parts = email.Split('@');
            if (parts.Length != 2 || !parts[1].Contains('.'))
                return new ValidationResult("E-mail inválido (domínio incompleto).");
        }
        catch
        {
            return new ValidationResult("Erro ao validar e-mail.");
        }

        return ValidationResult.Success;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}