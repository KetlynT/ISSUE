namespace GraficaModerna.Domain.Entities;

public class Coupon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; } // 0 a 100
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Construtor vazio para EF
    public Coupon() { }

    public Coupon(string code, decimal percentage, int daysValid)
    {
        Id = Guid.NewGuid();
        Code = code.ToUpper().Trim();
        DiscountPercentage = percentage;
        ExpiryDate = DateTime.UtcNow.AddDays(daysValid);
        IsActive = true;
    }

    public bool IsValid()
    {
        return IsActive && DateTime.UtcNow <= ExpiryDate;
    }
}