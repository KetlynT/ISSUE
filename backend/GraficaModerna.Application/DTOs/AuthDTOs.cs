using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record LoginDto([Required, EmailAddress] string Email, [Required] string Password);

public record RegisterDto(
    [Required(ErrorMessage = "Nome completo é obrigatório")] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required(ErrorMessage = "Telefone é obrigatório")] string PhoneNumber // NOVO
);

public record AuthResponseDto(string Token, string Email, string Role);