namespace GraficaModerna.Domain.Entities;

public class OrderHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty; // O status naquele momento
    public string? Message { get; set; } // Ex: "Pagamento confirmado", "Cancelado pelo Admin"
    public string ChangedBy { get; set; } = string.Empty; // UserId ou "SYSTEM" ou "STRIPE-WEBHOOK"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Auditoria técnica do momento da ação
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Propriedade de navegação (opcional, dependendo do mapeamento)
    // public Order Order { get; set; } 
}