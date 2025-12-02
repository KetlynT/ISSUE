using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICouponService _couponService;

    public OrderService(AppDbContext context, IEmailService emailService, ICouponService couponService)
    {
        _context = context;
        _emailService = emailService;
        _couponService = couponService;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(string userId, string shippingAddress, string shippingZip, string? couponCode)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Recupera o Carrinho
            var cart = await _context.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any()) throw new Exception("Carrinho vazio.");

            decimal subTotal = 0;
            var orderItems = new List<OrderItem>();

            // 2. Processa Itens e Estoque
            foreach (var item in cart.Items)
            {
                if (item.Product == null) continue;

                if (item.Product.StockQuantity < item.Quantity)
                    throw new Exception($"Estoque insuficiente para {item.Product.Name}.");

                item.Product.DebitStock(item.Quantity);
                subTotal += item.Quantity * item.Product.Price;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            // 3. Aplica Cupom
            decimal discount = 0;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _couponService.GetValidCouponAsync(couponCode);
                if (coupon != null) discount = subTotal * (coupon.DiscountPercentage / 100m);
            }

            // 4. Cria o Pedido
            var order = new Order
            {
                UserId = userId,
                ShippingAddress = shippingAddress,
                ShippingZipCode = shippingZip,
                Status = "Pendente",
                OrderDate = DateTime.UtcNow,
                SubTotal = subTotal,
                Discount = discount,
                TotalAmount = subTotal - discount,
                AppliedCoupon = !string.IsNullOrEmpty(couponCode) ? couponCode.ToUpper() : null,
                Items = orderItems
            };

            _context.Orders.Add(order);

            // 5. Limpa o Carrinho
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null) _ = _emailService.SendEmailAsync(user.Email!, "Pedido Confirmado", $"Seu pedido #{order.Id} foi recebido com sucesso.");
            }
            catch { }

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
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
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

    // ATUALIZADO: Agora aceita TrackingCode
    public async Task UpdateOrderStatusAsync(Guid orderId, string status, string? trackingCode)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;

            // Só atualiza o código se ele for fornecido (não sobrescreve com null se já existir)
            if (!string.IsNullOrEmpty(trackingCode))
            {
                order.TrackingCode = trackingCode;
            }

            await _context.SaveChangesAsync();

            // Lógica de notificação simplificada (pode ser expandida)
            if (status == "Enviado" && !string.IsNullOrEmpty(trackingCode))
            {
                // Enviar email com código de rastreio...
            }
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderDate,
            order.TotalAmount,
            order.Status,
            order.TrackingCode, // Mapeia o novo campo
            order.ShippingAddress,
            order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList()
        );
    }
}