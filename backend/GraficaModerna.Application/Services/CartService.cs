using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;

namespace GraficaModerna.Application.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICouponService _couponService;

    public CartService(AppDbContext context, IEmailService emailService, ICouponService couponService)
    {
        _context = context;
        _emailService = emailService;
        _couponService = couponService;
    }

    private async Task<Cart> GetCartEntity(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await GetCartEntity(userId);
        var itemsDto = cart.Items.Select(i => new CartItemDto(
            i.Id, i.ProductId, i.Product?.Name ?? "Indisponível", i.Product?.ImageUrl ?? "",
            i.Product?.Price ?? 0, i.Quantity, (i.Product?.Price ?? 0) * i.Quantity
        )).ToList();
        return new CartDto(cart.Id, itemsDto, itemsDto.Sum(i => i.TotalPrice));
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null) throw new Exception("Produto não encontrado.");
        if (product.StockQuantity < dto.Quantity) throw new Exception($"Estoque insuficiente. Restam {product.StockQuantity}.");

        var cart = await GetCartEntity(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existing != null)
        {
            if (product.StockQuantity < (existing.Quantity + dto.Quantity))
                throw new Exception("Estoque insuficiente para adicionar mais.");
            existing.Quantity += dto.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = dto.ProductId, Quantity = dto.Quantity });
        }
        cart.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(string userId, Guid itemId)
    {
        var cart = await GetCartEntity(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null) { _context.CartItems.Remove(item); await _context.SaveChangesAsync(); }
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await GetCartEntity(userId);
        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();
    }

    public async Task<OrderDto> CheckoutAsync(string userId, string shippingAddress, string shippingZip, string? couponCode)
    {
        var cart = await _context.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any()) throw new Exception("Carrinho vazio.");

        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();

        // Valida e debita estoque
        foreach (var item in cart.Items)
        {
            if (item.Product == null) continue;
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

        // Aplica Cupom
        decimal discount = 0;
        if (!string.IsNullOrEmpty(couponCode))
        {
            var coupon = await _couponService.GetValidCouponAsync(couponCode);
            if (coupon != null) discount = subTotal * (coupon.DiscountPercentage / 100m);
        }

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
        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();

        // Envia Email
        var user = await _context.Users.FindAsync(userId);
        if (user != null) _ = _emailService.SendEmailAsync(user.Email!, "Pedido Recebido", $"Seu pedido #{order.Id} foi recebido.");

        return new OrderDto(
            order.Id, order.OrderDate, order.TotalAmount, order.Status, order.ShippingAddress,
            order.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList()
        );
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        var orders = await _context.Orders.Include(o => o.Items).Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).ToListAsync();
        return MapToDto(orders);
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders.Include(o => o.Items).OrderByDescending(o => o.OrderDate).ToListAsync();
        return MapToDto(orders);
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null) { order.Status = status; await _context.SaveChangesAsync(); }
    }

    private List<OrderDto> MapToDto(List<Order> orders)
    {
        return orders.Select(o => new OrderDto(
            o.Id, o.OrderDate, o.TotalAmount, o.Status, o.ShippingAddress,
            o.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList()
        )).ToList();
    }
}