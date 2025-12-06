using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // NOVOS CAMPOS PARA REFRESH TOKEN
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}