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

        // SEGURANÇA: Prioriza Variável de Ambiente em Produção
        var apiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? _configuration["Stripe:SecretKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("FATAL: Stripe Secret Key não configurada (STRIPE_SECRET_KEY).");
        }

        StripeConfiguration.ApiKey = apiKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(Order order)
    {
        // Flexibilidade para Deploy (Docker/Azure/AWS usam Env Vars)
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                          ?? _configuration["FrontendUrl"]
                          ?? "http://localhost:5173";

        // Remove barras finais se existirem para evitar "//"
        frontendUrl = frontendUrl.TrimEnd('/');

        var successUrl = $"{frontendUrl}/sucesso?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{frontendUrl}/meus-pedidos";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "order_id", order.Id.ToString() },
                { "user_email", order.UserId },
                { "expected_amount", order.TotalAmount.ToString("F2") } // Útil para auditoria
            },
            LineItems = new List<SessionLineItemOptions>()
        };

        foreach (var item in order.Items)
        {
            var unitAmount = (long)Math.Round(item.UnitPrice * 100);

            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = unitAmount,
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
            var shippingAmount = (long)Math.Round(order.ShippingCost * 100);
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = shippingAmount,
                    Currency = "brl",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Frete - {order.ShippingMethod}",
                    },
                },
                Quantity = 1,
            });
        }

        // Lógica de Cupom (One-time usage)
        if (order.Discount > 0)
        {
            // Cria um cupom dinâmico de uso único no Stripe
            var couponOptions = new CouponCreateOptions
            {
                AmountOff = (long)Math.Round(order.Discount * 100),
                Currency = "brl",
                Duration = "once",
                Name = order.AppliedCoupon ?? "Desconto Promocional"
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

    public async Task RefundPaymentAsync(string paymentIntentId)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
            throw new ArgumentException("ID da transação inválido para reembolso.");

        var refundService = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Reason = RefundReasons.RequestedByCustomer
        };

        await refundService.CreateAsync(options);
    }
}