using System.Data;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService(
    AppDbContext context,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IEnumerable<IShippingService> shippingServices,
    IPaymentService paymentService) : IOrderService
{
    private readonly AppDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IEnumerable<IShippingService> _shippingServices = shippingServices;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode,
        string shippingMethod)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || cart.Items.Count == 0)
            throw new Exception("Carrinho vazio.");

        if (cart.Items.Any(i => i.Quantity <= 0))
            throw new Exception("O carrinho contém itens com quantidades inválidas.");

        var shippingItems = cart.Items.Select(i => new ShippingItemDto
        {
            ProductId = i.ProductId,
            Weight = i.Product!.Weight,
            Width = i.Product.Width,
            Height = i.Product.Height,
            Length = i.Product.Length,
            Quantity = i.Quantity
        }).ToList();

        var shippingTasks = _shippingServices.Select(s => s.CalculateAsync(addressDto.ZipCode, shippingItems));
        var shippingResults = await Task.WhenAll(shippingTasks);
        var allOptions = shippingResults.SelectMany(x => x).ToList();

        var selectedOption = allOptions.FirstOrDefault(o =>
                                 o.Name.Trim().Equals(shippingMethod.Trim(),
                                     StringComparison.InvariantCultureIgnoreCase)) ??
                             throw new Exception("Método de envio inválido ou indisponível.");
        var verifiedShippingCost = selectedOption.Price;

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;

                if (item.Product.StockQuantity < item.Quantity)
                    throw new Exception($"Estoque insuficiente para o produto {item.Product.Name}");

                item.Product.DebitStock(item.Quantity);
                _context.Entry(item.Product).State = EntityState.Modified;

                subTotal += item.Quantity * item.Product.Price;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                    c.Code.Equals(couponCode, StringComparison.CurrentCultureIgnoreCase));
                if (coupon != null && coupon.IsValid())
                {
                    var alreadyUsed =
                        await _context.CouponUsages.AnyAsync(u => u.UserId == userId && u.CouponCode == coupon.Code);
                    if (alreadyUsed) throw new Exception("Cupom já utilizado.");

                    discount = subTotal * (coupon.DiscountPercentage / 100m);
                }
            }

            var totalAmount = subTotal - discount + verifiedShippingCost;

            if (totalAmount < Order.MinOrderAmount)
                throw new Exception($"O valor total do pedido deve ser no mínimo {Order.MinOrderAmount:C}.");

            if (totalAmount > Order.MaxOrderAmount)
                throw new Exception($"O valor do pedido excede o limite de segurança de {Order.MaxOrderAmount:C}.");

            var formattedAddress =
                $"{addressDto.Street}, {addressDto.Number} - {addressDto.Complement} - {addressDto.Neighborhood}, " +
                $"{addressDto.City}/{addressDto.State} (Ref: {addressDto.Reference}) - A/C: {addressDto.ReceiverName} - Tel: {addressDto.PhoneNumber}";

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = formattedAddress,
                ShippingZipCode = addressDto.ZipCode,
                ShippingCost = verifiedShippingCost,
                ShippingMethod = selectedOption.Name,
                Status = "Pendente",
                OrderDate = DateTime.UtcNow,
                SubTotal = subTotal,
                Discount = discount,
                TotalAmount = totalAmount,
                AppliedCoupon = couponCode?.ToUpper(),
                Items = orderItems,
                CustomerIp = GetIpAddress(),
                UserAgent = GetUserAgent()
            };

            order.History.Add(new OrderHistory
            {
                Status = "Pendente",
                Message = "Pedido criado via Checkout",
                ChangedBy = userId,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent()
            });

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (discount > 0 && couponCode != null)
                _context.CouponUsages.Add(new CouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode.ToUpper(),
                    OrderId = order.Id,
                    UsedAt = DateTime.UtcNow
                });

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _ = SendOrderReceivedEmailAsync(userId, order.Id);

            return MapToDto(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return [.. orders.Select(MapToDto)];
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return [.. orders.Select(MapToDto)];
    }

    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var adminUserId = _userManager.GetUserId(user!) ?? "AdminUnknown";

        if (user == null || !user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Apenas administradores podem alterar pedidos.");

        var order = await _context.Orders
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new Exception("Pedido não encontrado");
        var auditMessage = $"Status alterado manualmente para {dto.Status}";

        if (dto.Status == "Aguardando Devolução")
        {
            if (!string.IsNullOrEmpty(dto.ReverseLogisticsCode))
                order.ReverseLogisticsCode = dto.ReverseLogisticsCode;

            order.ReturnInstructions = !string.IsNullOrEmpty(dto.ReturnInstructions)
                ? dto.ReturnInstructions
                : "Instruções padrão de devolução...";

            auditMessage += ". Instruções geradas.";
        }

        if (dto.Status == "Reembolsado" || dto.Status == "Cancelado")
            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
                try
                {
                    await _paymentService.RefundPaymentAsync(order.StripePaymentIntentId);
                    auditMessage += ". Reembolso processado no Stripe.";
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro no reembolso Stripe: {ex.Message}");
                }

        if (dto.Status == "Entregue" && order.Status != "Entregue")
            order.DeliveryDate = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.TrackingCode))
        {
            order.TrackingCode = dto.TrackingCode;
            auditMessage += $" (Rastreio: {dto.TrackingCode})";
        }

        AddAuditLog(order, dto.Status, auditMessage, $"Admin:{adminUserId}");

        await _context.SaveChangesAsync();
    }

    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId, long amountPaidInCents)
    {
        var isAlreadyProcessed = await _context.Orders
            .AnyAsync(o => o.StripePaymentIntentId == transactionId);

        if (isAlreadyProcessed)
            return;

        var order = await _context.Orders
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return;

        var expectedAmountInCents = (long)(order.TotalAmount * 100);

        if (amountPaidInCents != expectedAmountInCents)
        {
            AddAuditLog(order, "Fraude Suspeita",
                $"Divergência de valor. Esperado: {expectedAmountInCents}, Recebido: {amountPaidInCents}. Transação: {transactionId}",
                "SYSTEM-SECURITY");

            await _context.SaveChangesAsync();

            throw new Exception(
                $"FATAL: Tentativa de manipulação de pagamento. Pedido {orderId}. Esperado {expectedAmountInCents}, Recebido {amountPaidInCents}");
        }

        if (order.Status != "Pago")
        {
            order.StripePaymentIntentId = transactionId;

            AddAuditLog(order, "Pago",
                $"Pagamento confirmado via Webhook. ID: {transactionId}. Valor validado: {amountPaidInCents / 100.0:C}",
                "STRIPE-WEBHOOK");

            await _context.SaveChangesAsync();
        }
    }

    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await GetUserOrderOrFail(orderId, userId);

        AddAuditLog(order, "Reembolso Solicitado", "Usuário solicitou cancelamento pelo painel.", userId);

        await _context.SaveChangesAsync();
    }

    private string GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }

    private string GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown";
    }

    private void AddAuditLog(Order order, string newStatus, string message, string changedBy)
    {
        order.Status = newStatus;

        order.History.Add(new OrderHistory
        {
            OrderId = order.Id,
            Status = newStatus,
            Message = message,
            ChangedBy = changedBy,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });
    }

    private async Task<Order> GetUserOrderOrFail(Guid orderId, string userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.History) 
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new Exception("Pedido não encontrado.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para acessar este pedido.");

        return order;
    }

    private async Task SendOrderReceivedEmailAsync(string userId, Guid orderId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _emailService.SendEmailAsync(user.Email!, "Pedido Recebido",
                    $"Seu pedido #{orderId} foi recebido e está sendo processado.");
        }
        catch
        {
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderDate,
            order.DeliveryDate,
            order.SubTotal,
            order.Discount,
            order.ShippingCost,
            order.TotalAmount,
            order.Status,
            order.TrackingCode,
            order.ReverseLogisticsCode,
            order.ReturnInstructions,
            order.ShippingAddress,
            [
                .. order.Items.Select(i =>
                    new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)
                )
            ]
        );
    }
}