using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(14)]
    public string CpfCnpj { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}