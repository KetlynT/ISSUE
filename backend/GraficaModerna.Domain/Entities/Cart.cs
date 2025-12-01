namespace GraficaModerna.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Vincula ao usuário logado

    // Navegação
    public List<CartItem> Items { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}