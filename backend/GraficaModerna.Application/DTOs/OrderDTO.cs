using System.ComponentModel.DataAnnotations;

namespace GraficaModerna.Application.DTOs;

public record OrderDto(
    Guid Id,
    DateTime OrderDate,
    DateTime? DeliveryDate,
    decimal SubTotal,
    decimal Discount,
    decimal ShippingCost,
    decimal TotalAmount,
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string? RefundRejectionReason,
    string? RefundRejectionProof,
    string ShippingAddress,
    string CustomerName,
    List<OrderItemDto> Items,
    string? PaymentWarning
);

public record AdminOrderDto(
    Guid Id,
    DateTime OrderDate,
    DateTime? DeliveryDate,
    decimal SubTotal,
    decimal Discount,
    decimal ShippingCost,
    decimal TotalAmount,
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string? RefundRejectionReason,
    string? RefundRejectionProof,
    string ShippingAddress,
    string CustomerName,
    string CustomerCpfMasked,
    string CustomerEmail,
    string? CustomerIpMasked,
    List<OrderItemDto> Items,
    List<OrderHistoryDto> AuditTrail
);

public record OrderHistoryDto(
    string Status,
    string? Message,
    string ChangedBy,
    DateTime Timestamp
);

public record OrderItemDto(
    string ProductName, 
    int Quantity,
    int RefundQuantity,
    decimal UnitPrice, 
    decimal Total);

public record UpdateOrderStatusDto(
    string Status,
    string? TrackingCode,
    string? ReverseLogisticsCode,
    string? ReturnInstructions,
    string? RefundRejectionReason,
    string? RefundRejectionProof,
    decimal? RefundAmount
);

public record RequestRefundDto(
    string RefundType,
    List<RefundItemRequestDto>? Items
);

public record RefundItemRequestDto(
    Guid ProductId,
    int Quantity
);

public static class DataMaskingExtensions
{
    public static string MaskCpfCnpj(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return "N/A";

        var clean = new string(document.Where(char.IsDigit).ToArray());

        if (clean.Length == 11)
            return $"XXX.XXX.XXX-{clean[^2..]}";

        if (clean.Length == 14)
            return $"XX.XXX.XXX/XXXX-{clean[^2..]}";

        return "***";
    }

    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***@***";

        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length > 2
            ? $"{localPart[0]}***{localPart[^1]}"
            : "***";

        return $"{maskedLocal}@{domain}";
    }

    public static string MaskIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return "N/A";

        var parts = ip.Split('.');
        if (parts.Length == 4)
            return $"{parts[0]}.{parts[1]}.{parts[2]}.XXX";

        var ipv6Parts = ip.Split(':');
        if (ipv6Parts.Length >= 4)
            return $"{string.Join(":", ipv6Parts.Take(3))}:XXXX";

        return "XXX.XXX.XXX.XXX";
    }
}