using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record RegisterDto(
    [Required(ErrorMessage = "O nome completo é obrigatório")] string FullName,
    [Required(ErrorMessage = "O email é obrigatório")][EmailAddress] string Email,
    [Required(ErrorMessage = "A senha é obrigatória")][MinLength(6)] string Password,
    [Required(ErrorMessage = "A confirmação de senha é obrigatória")][property: Compare("Password")] string ConfirmPassword,
    [Required(ErrorMessage = "CPF/CNPJ é obrigatório")] string CpfCnpj,
    string? PhoneNumber
);

public record LoginDto(
    [Required(ErrorMessage = "O email é obrigatório")][EmailAddress] string Email,
    [Required(ErrorMessage = "A senha é obrigatória")] string Password,
    bool IsAdminLogin = false
);
public record ForgotPasswordDto([Required][EmailAddress] string Email);

public record ResetPasswordDto(
    [Required][EmailAddress] string Email,
    [Required] string Token,
    [Required][MinLength(6)] string NewPassword
);

public record ConfirmEmailDto(
    [Required] string UserId,
    [Required] string Token
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Role
);

public record TokenModel(string? AccessToken, string? RefreshToken);