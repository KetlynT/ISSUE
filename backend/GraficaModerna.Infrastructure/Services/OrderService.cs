using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEnumerable<IShippingService> _shippingServices;
    private readonly IPaymentService _paymentService;

    // Constantes de Validação
    private const decimal MinOrderAmount = 1.00m;      // Mínimo R$ 1,00
    private const decimal MaxOrderAmount = 100000.00m; // Máximo R$ 100.000,00

    public OrderService(
        AppDbContext context,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        IEnumerable<IShippingService> shippingServices,
        IPaymentService paymentService)
    {
        _context = context;
        _emailService = emailService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _shippingServices = shippingServices;
        _paymentService = paymentService;
    }

    // =================================================================================
    // MÉTODOS AUXILIARES DE AUDITORIA E SEGURANÇA
    // =================================================================================

    private string GetIpAddress() =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";

    private string GetUserAgent() =>
        _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

    /// <summary>
    /// Centraliza a alteração de status para garantir que o histórico seja sempre gerado.
    /// </summary>
    private void AddAuditLog(Order order, string newStatus, string message, string changedBy)
    {
        // Atualiza o status principal do pedido
        order.Status = newStatus;

        // Adiciona o registro no histórico
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
            .Include(o => o.History) // Importante carregar o histórico
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para acessar este pedido.");

        return order;
    }

    // =================================================================================
    // FUNCIONALIDADES PRINCIPAIS
    // =================================================================================

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, string shippingMethod)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            throw new Exception("Carrinho vazio.");

        // Validação de itens inválidos
        if (cart.Items.Any(i => i.Quantity <= 0))
            throw new Exception("O carrinho contém itens com quantidades inválidas.");

        // 1. Recalcular frete no backend (Segurança contra manipulação no front)
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
            o.Name.Trim().Equals(shippingMethod.Trim(), StringComparison.InvariantCultureIgnoreCase));

        if (selectedOption == null)
            throw new Exception("Método de envio inválido ou indisponível.");

        decimal verifiedShippingCost = selectedOption.Price;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;

                if (item.Product.StockQuantity < item.Quantity)
                    throw new Exception($"Estoque insuficiente para o produto {item.Product.Name}");

                // Baixa no estoque
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
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper());
                if (coupon != null && coupon.IsValid())
                {
                    bool alreadyUsed = await _context.CouponUsages.AnyAsync(u => u.UserId == userId && u.CouponCode == coupon.Code);
                    if (alreadyUsed) throw new Exception("Cupom já utilizado.");

                    discount = subTotal * (coupon.DiscountPercentage / 100m);
                }
            }

            decimal totalAmount = (subTotal - discount) + verifiedShippingCost;

            // Validação de Limites Financeiros
            if (totalAmount < MinOrderAmount)
                throw new Exception($"O valor total do pedido deve ser no mínimo {MinOrderAmount:C}.");

            if (totalAmount > MaxOrderAmount)
                throw new Exception($"O valor do pedido excede o limite de segurança de {MaxOrderAmount:C}.");

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

                // Auditoria Inicial
                CustomerIp = GetIpAddress(),
                UserAgent = GetUserAgent()
            };

            // Adiciona o primeiro log no histórico
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

            // Registra uso do cupom se houver
            if (discount > 0 && couponCode != null)
            {
                _context.CouponUsages.Add(new CouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode.ToUpper(),
                    OrderId = order.Id,
                    UsedAt = DateTime.UtcNow
                });
            }

            // Limpa o carrinho
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Dispara email em background
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

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var adminUserId = _userManager.GetUserId(user!) ?? "AdminUnknown";

        if (user == null || !user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Apenas administradores podem alterar pedidos.");

        var order = await _context.Orders
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("Pedido não encontrado");

        string auditMessage = $"Status alterado manualmente para {dto.Status}";

        // Lógica específica por status
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
        {
            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                try
                {
                    await _paymentService.RefundPaymentAsync(order.StripePaymentIntentId);
                    auditMessage += ". Reembolso processado no Stripe.";
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro no reembolso Stripe: {ex.Message}");
                }
            }
        }

        if (dto.Status == "Entregue" && order.Status != "Entregue")
            order.DeliveryDate = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.TrackingCode))
        {
            order.TrackingCode = dto.TrackingCode;
            auditMessage += $" (Rastreio: {dto.TrackingCode})";
        }

        // Aplica a auditoria e mudança de status
        AddAuditLog(order, dto.Status, auditMessage, $"Admin:{adminUserId}");

        await _context.SaveChangesAsync();
    }

    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId)
    {
        var order = await _context.Orders
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return;

        if (order.Status != "Pago")
        {
            order.StripePaymentIntentId = transactionId;

            // Webhooks são chamados pelo sistema, usamos uma constante para identificar
            AddAuditLog(order, "Pago", $"Pagamento confirmado via Webhook. ID: {transactionId}", "STRIPE-WEBHOOK");

            await _context.SaveChangesAsync();
        }
    }

    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await GetUserOrderOrFail(orderId, userId);

        AddAuditLog(order, "Reembolso Solicitado", "Usuário solicitou cancelamento pelo painel.", userId);

        await _context.SaveChangesAsync();
    }

    private async Task SendOrderReceivedEmailAsync(string userId, Guid orderId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _emailService.SendEmailAsync(user.Email!, "Pedido Recebido", $"Seu pedido #{orderId} foi recebido e está sendo processado.");
        }
        catch
        {
            // Log de erro de envio de email (serilog, etc)
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
            order.Items.Select(i =>
                new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)
            ).ToList()
        );
    }
}