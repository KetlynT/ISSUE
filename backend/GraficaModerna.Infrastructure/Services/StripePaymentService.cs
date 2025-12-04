using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(Order order)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{frontendUrl}/sucesso?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{frontendUrl}/meus-pedidos",
            Metadata = new Dictionary<string, string>
            {
                { "order_id", order.Id.ToString() },
                { "user_email", order.UserId }
            },
            LineItems = new List<SessionLineItemOptions>()
        };

        foreach (var item in order.Items)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmountDecimal = item.UnitPrice * 100,
                    Currency = "brl",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                    },
                },
                Quantity = item.Quantity,
            });
        }

        if (order.ShippingCost > 0)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmountDecimal = order.ShippingCost * 100,
                    Currency = "brl",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Frete - {order.ShippingMethod}",
                    },
                },
                Quantity = 1,
            });
        }

        if (order.Discount > 0)
        {
            var couponOptions = new CouponCreateOptions
            {
                AmountOff = (long)(order.Discount * 100),
                Currency = "brl",
                Duration = "once",
                Name = order.AppliedCoupon ?? "Desconto"
            };

            var stripeCouponService = new Stripe.CouponService();
            var stripeCoupon = await stripeCouponService.CreateAsync(couponOptions);

            options.Discounts = new List<SessionDiscountOptions>
            {
                new SessionDiscountOptions { Coupon = stripeCoupon.Id }
            };
        }

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return session.Url;
    }

    // --- NOVO: Implementação do Reembolso ---
    public async Task RefundPaymentAsync(string paymentIntentId)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
            throw new ArgumentException("ID da transação inválido para reembolso.");

        var refundService = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Reason = RefundReasons.RequestedByCustomer // Ou 'duplicate', 'fraudulent'
        };

        try
        {
            await refundService.CreateAsync(options);
        }
        catch (StripeException ex)
        {
            // Se já foi reembolsado, o Stripe lança erro. Podemos tratar ou repassar.
            // Aqui vamos lançar uma exceção amigável.
            throw new Exception($"Erro ao processar reembolso no Stripe: {ex.StripeError.Message}");
        }
    }
}