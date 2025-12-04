using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }

    // Valores
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingCost { get; set; }
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    public string? AppliedCoupon { get; set; }
    public string Status { get; set; } = "Pendente";
    public string? TrackingCode { get; set; }

    // DADOS DO STRIPE (NOVOS)
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    // Logística Reversa
    public string? ReverseLogisticsCode { get; set; }
    public string? ReturnInstructions { get; set; }

    // Endereço
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;

    // SEGURANÇA & AUDITORIA
    public string? CustomerIp { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}