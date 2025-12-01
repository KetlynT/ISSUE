namespace GraficaModerna.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Valores
    public decimal SubTotal { get; set; } // Valor dos produtos
    public decimal Discount { get; set; } // Valor abatido
    public decimal TotalAmount { get; set; } // Valor final (SubTotal - Discount)
    public string? AppliedCoupon { get; set; } // Código do cupom usado

    public string Status { get; set; } = "Pendente";

    // Endereço (Snapshot)
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = new();
}