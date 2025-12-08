using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace GraficaModerna.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly MetadataSecurityService _securityService;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger, MetadataSecurityService securityService)
    {
        _configuration = configuration;
        _logger = logger;
        _securityService = securityService;

        var apiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? _configuration["Stripe:SecretKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogCritical("STRIPE_SECRET_KEY não encontrada.");
            throw new Exception("Erro de configuração no servidor de pagamentos.");
        }

        StripeConfiguration.ApiKey = apiKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(Order order)
    {
        try
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                              ?? _configuration["FrontendUrl"]
                              ?? "http://localhost:5173";

            frontendUrl = frontendUrl.TrimEnd('/');

            var successUrl = $"{frontendUrl}/sucesso?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{frontendUrl}/meus-pedidos";

            var (encryptedOrder, signature) = _securityService.Protect(order.Id.ToString());

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "order_data", encryptedOrder },
                    { "sig", signature }
                },
                LineItems = []
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
                            Name = item.ProductName
                        }
                    },
                    Quantity = item.Quantity
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
                            Name = $"Frete - {order.ShippingMethod}"
                        }
                    },
                    Quantity = 1
                });
            }

            if (order.Discount > 0)
            {
                var couponOptions = new CouponCreateOptions
                {
                    AmountOff = (long)Math.Round(order.Discount * 100),
                    Currency = "brl",
                    Duration = "once",
                    Name = order.AppliedCoupon ?? "Desconto Promocional"
                };

                var stripeCouponService = new Stripe.CouponService();
                var stripeCoupon = await stripeCouponService.CreateAsync(couponOptions);

                options.Discounts =
                [
                    new SessionDiscountOptions { Coupon = stripeCoupon.Id }
                ];
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }
        catch (StripeException stripeEx)
        {
            var requestId = stripeEx.StripeResponse?.RequestId ?? "N/A";

            _logger.LogError(stripeEx,
                "Erro Stripe ao criar sessão. OrderId: {OrderId}, StripeCode: {StripeCode}, RequestId: {RequestId}",
                order.Id, stripeEx.StripeError?.Code, requestId);

            throw new Exception("Falha na comunicação com o provedor de pagamentos. Tente novamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro genérico ao criar sessão de pagamento. OrderId: {OrderId}", order.Id);
            throw new Exception("Erro ao processar o pagamento.");
        }
    }

    public async Task RefundPaymentAsync(string paymentIntentId)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
            throw new ArgumentException("ID da transação inválido.");

        try
        {
            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Reason = RefundReasons.RequestedByCustomer
            };

            await refundService.CreateAsync(options);
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx,
                "Erro Stripe ao processar reembolso. PaymentIntent: {PaymentIntent}, Code: {Code}",
                paymentIntentId, stripeEx.StripeError?.Code);

            throw new Exception("Não foi possível processar o reembolso automático no provedor de pagamentos.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro genérico ao processar reembolso. PaymentIntent: {PaymentIntent}",
                paymentIntentId);
            throw new Exception("Erro ao processar reembolso.");
        }
    }
}