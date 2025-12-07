using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}
