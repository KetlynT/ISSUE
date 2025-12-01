namespace GraficaModerna.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    // Salvamos o nome e preço DA ÉPOCA da compra, pois o produto pode mudar depois
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; } // Opcional, para linkar ao produto atual
}