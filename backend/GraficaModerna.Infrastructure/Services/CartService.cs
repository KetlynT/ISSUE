using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using GraficaModerna.Infrastructure.Context;

namespace GraficaModerna.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _context;

    public CartService(IUnitOfWork uow, AppDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    private async Task<Cart> GetOrCreateCart(string userId)
    {
        var cart = await _uow.Carts.GetByUserIdAsync(userId);
        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                LastUpdated = DateTime.UtcNow
            };
            await _uow.Carts.AddAsync(cart);
            await _uow.CommitAsync();
        }
        return cart;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await GetOrCreateCart(userId);

        // Proteção contra produtos deletados
        var itemsDto = cart.Items
            .Where(i => i.Product != null && i.Product.IsActive)
            .Select(i => new CartItemDto(
                i.Id,
                i.ProductId,
                i.Product!.Name,
                i.Product.ImageUrl ?? "",
                i.Product.Price,
                i.Quantity,
                i.Product.Price * i.Quantity,
                i.Product.Weight,
                i.Product.Width,
                i.Product.Height,
                i.Product.Length
            )).ToList();

        // Limpar itens órfãos ou inativos
        var orphanedItems = cart.Items.Where(i => i.Product == null || !i.Product.IsActive).ToList();
        if (orphanedItems.Any())
        {
            foreach (var item in orphanedItems)
            {
                await _uow.Carts.RemoveItemAsync(item);
            }
            await _uow.CommitAsync();
        }

        return new CartDto(cart.Id, itemsDto, itemsDto.Sum(i => i.TotalPrice));
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        // CORREÇÃO: Usar transação
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // CORREÇÃO: Lock no produto para evitar race condition
            var product = await _context.Products
                .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = {0} FOR UPDATE", dto.ProductId)
                .FirstOrDefaultAsync();

            if (product == null || !product.IsActive)
            {
                throw new Exception("Produto indisponível ou removido.");
            }

            if (product.StockQuantity < dto.Quantity)
            {
                throw new Exception($"Estoque insuficiente. Disponível: {product.StockQuantity} unidades.");
            }

            var cart = await GetOrCreateCart(userId);
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (existing != null)
            {
                int newTotal = existing.Quantity + dto.Quantity;

                if (product.StockQuantity < newTotal)
                {
                    throw new Exception($"Não é possível adicionar mais itens. Estoque disponível: {product.StockQuantity}");
                }

                existing.Quantity = newTotal;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            cart.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveItemAsync(userId, cartItemId);
            return;
        }

        // CORREÇÃO: Usar transação
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var cart = await GetOrCreateCart(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item == null)
            {
                throw new Exception("Item não encontrado no carrinho.");
            }

            // CORREÇÃO: Lock no produto
            var product = await _context.Products
                .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = {0} FOR UPDATE", item.ProductId)
                .FirstOrDefaultAsync();

            if (product == null || !product.IsActive)
            {
                throw new Exception("Produto indisponível.");
            }

            if (product.StockQuantity < quantity)
            {
                throw new Exception($"Estoque insuficiente. Disponível: {product.StockQuantity}");
            }

            item.Quantity = quantity;
            cart.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RemoveItemAsync(string userId, Guid itemId)
    {
        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

        if (item != null)
        {
            await _uow.Carts.RemoveItemAsync(item);
            await _uow.CommitAsync();
        }
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await _uow.Carts.GetByUserIdAsync(userId);
        if (cart != null)
        {
            await _uow.Carts.ClearCartAsync(cart.Id);
            await _uow.CommitAsync();
        }
    }
}