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

    // 🔐 CENTRALIZA A SEGURANÇA
    private async Task<Order> GetUserOrderOrFail(Guid orderId, string userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Pedido não encontrado.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para acessar este pedido.");

        return order;
    }

    // ===========================
    // CRIAÇÃO DO PEDIDO
    // ===========================
    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, CreateAddressDto addressDto, string? couponCode, string shippingMethod)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            throw new Exception("Carrinho vazio.");

        // 1. Recalcular frete no backend
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

        // 2. Transação + Estoque
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;

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

            // 3. Cupom
            decimal discount = 0;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper());

                if (coupon != null && coupon.IsValid())
                {
                    bool alreadyUsed = await _context.CouponUsages.AnyAsync(u => u.UserId == userId && u.CouponCode == coupon.Code);

                    if (alreadyUsed)
                        throw new Exception("Cupom já utilizado.");

                    discount = subTotal * (coupon.DiscountPercentage / 100m);
                }
            }

            decimal totalAmount = (subTotal - discount) + verifiedShippingCost;

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
                CustomerIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Email não trava execução
            _ = SendOrderReceivedEmailAsync(userId, order.Id);

            return MapToDto(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task SendOrderReceivedEmailAsync(string userId, Guid orderId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _emailService.SendEmailAsync(user.Email!, "Pedido Recebido", $"Seu pedido #{orderId} foi recebido.");
        }
        catch { }
    }

    // ===========================
    // CONSULTAS
    // ===========================
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

    // ===========================
    // ADMIN
    // ===========================
    public async Task UpdateAdminOrderAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null || !user.IsInRole("Admin"))
            throw new UnauthorizedAccessException("Apenas administradores podem alterar pedidos.");

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) throw new Exception("Pedido não encontrado");

        if (dto.Status == "Aguardando Devolução")
        {
            if (!string.IsNullOrEmpty(dto.ReverseLogisticsCode))
                order.ReverseLogisticsCode = dto.ReverseLogisticsCode;

            order.ReturnInstructions =
                !string.IsNullOrEmpty(dto.ReturnInstructions)
                    ? dto.ReturnInstructions
                    : "Instruções padrão...";
        }

        if (dto.Status == "Reembolsado" || dto.Status == "Cancelado")
        {
            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                try
                {
                    await _paymentService.RefundPaymentAsync(order.StripePaymentIntentId);
                    order.ReturnInstructions += " [Reembolso Stripe OK]";
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
            order.TrackingCode = dto.TrackingCode;

        order.Status = dto.Status;

        await _context.SaveChangesAsync();
    }

    // ===========================
    // WEBHOOK STRIPE
    // ===========================
    public async Task ConfirmPaymentViaWebhookAsync(Guid orderId, string transactionId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return;

        if (order.Status != "Pago")
        {
            order.Status = "Pago";
            order.StripePaymentIntentId = transactionId;
            await _context.SaveChangesAsync();
        }
    }

    // ===========================
    // USUÁRIO: pagar
    // ===========================
    public async Task PayOrderAsync(Guid orderId, string userId)
    {
        var order = await GetUserOrderOrFail(orderId, userId);

        order.Status = "Pago";
        await _context.SaveChangesAsync();
    }

    // ===========================
    // USUÁRIO: solicitar reembolso
    // ===========================
    public async Task RequestRefundAsync(Guid orderId, string userId)
    {
        var order = await GetUserOrderOrFail(orderId, userId);

        order.Status = "Reembolso Solicitado";
        await _context.SaveChangesAsync();
    }

    // ===========================
    // MAP
    // ===========================
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
