using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
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

        // Mapeamento manual incluindo dados de frete
        var itemsDto = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? "Indisponível",
            i.Product?.ImageUrl ?? "",
            i.Product?.Price ?? 0,
            i.Quantity,
            (i.Product?.Price ?? 0) * i.Quantity,
            // Dados para cálculo de frete
            i.Product?.Weight ?? 0,
            i.Product?.Width ?? 0,
            i.Product?.Height ?? 0,
            i.Product?.Length ?? 0
        )).ToList();

        return new CartDto(cart.Id, itemsDto, itemsDto.Sum(i => i.TotalPrice));
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null) throw new Exception("Produto não encontrado.");
        if (!product.IsActive) throw new Exception("Produto indisponível.");
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

    // NOVO: Atualiza a quantidade exata (usado pelos botões + e -)
    public async Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveItemAsync(userId, cartItemId);
            return;
        }

        var cart = await GetCartEntity(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null) throw new Exception("Item não encontrado no carrinho.");
        if (item.Product == null) throw new Exception("Produto inválido.");

        if (item.Product.StockQuantity < quantity)
            throw new Exception($"Estoque insuficiente. Máximo disponível: {item.Product.StockQuantity}");

        item.Quantity = quantity;
        cart.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(string userId, Guid itemId)
    {
        var cart = await GetCartEntity(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await GetCartEntity(userId);
        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();
    }
}