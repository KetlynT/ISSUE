using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record UserProfileDto(
    string FullName,
    string Email,
    string? PhoneNumber,
    string? ZipCode,
    string? Address,
    string? City,
    string? State
);

public record UpdateProfileDto(
    [Required] string FullName,
    string? PhoneNumber,
    string? ZipCode,
    string? Address,
    string? City,
    string? State
);