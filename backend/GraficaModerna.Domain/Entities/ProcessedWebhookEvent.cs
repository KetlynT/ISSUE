using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Domain.Entities;

public class ProcessedWebhookEvent
{
    [Key]
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}