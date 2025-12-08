using System.ComponentModel.DataAnnotations.Schema;

namespace GraficaModerna.Domain.Entities;

public class Order
{
    public const decimal MinOrderAmount = 1.00m;
    public const decimal MaxOrderAmount = 100000.00m;

    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingCost { get; set; }
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    public string? AppliedCoupon { get; set; }
    public string Status { get; set; } = "Pendente";
    public string? TrackingCode { get; set; }

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public string? ReverseLogisticsCode { get; set; }
    public string? ReturnInstructions { get; set; }

    public string? RefundRejectionReason { get; set; }
    public string? RefundRejectionProof { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;

    public string? CustomerIp { get; set; }
    public string? UserAgent { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public List<OrderHistory> History { get; set; } = [];
}