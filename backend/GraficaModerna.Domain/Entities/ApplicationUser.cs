// Local: backend/GraficaModerna.Domain/Entities/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace GraficaModerna.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Novos campos para Perfil / Entrega
    public string? ZipCode { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}