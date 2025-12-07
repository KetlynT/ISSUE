using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record UserProfileDto(
    string FullName,
    string Email,
    string CpfCnpj,
    string? PhoneNumber
);

public record UpdateProfileDto(
    [Required] string FullName,
    [Required] string CpfCnpj,
    string? PhoneNumber
);
