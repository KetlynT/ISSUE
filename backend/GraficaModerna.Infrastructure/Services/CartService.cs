using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;

namespace GraficaModerna.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private async Task<Cart> GetOrCreateCart(string userId)
    {
        var cart = await _uow.Carts.GetByUserIdAsync(userId);
        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            await _uow.Carts.AddAsync(cart);
            await _uow.CommitAsync();
        }
        return cart;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await GetOrCreateCart(userId);

        var itemsDto = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? "Indisponível",
            i.Product?.ImageUrl ?? "",
            i.Product?.Price ?? 0,
            i.Quantity,
            (i.Product?.Price ?? 0) * i.Quantity,
            i.Product?.Weight ?? 0,
            i.Product?.Width ?? 0,
            i.Product?.Height ?? 0,
            i.Product?.Length ?? 0
        )).ToList();

        return new CartDto(cart.Id, itemsDto, itemsDto.Sum(i => i.TotalPrice));
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        var product = await _uow.Products.GetByIdAsync(dto.ProductId);
        if (product == null || !product.IsActive) throw new Exception("Produto indisponível.");
        if (product.StockQuantity < dto.Quantity) throw new Exception($"Estoque insuficiente. Restam {product.StockQuantity}.");

        var cart = await GetOrCreateCart(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existing != null)
        {
            if (product.StockQuantity < (existing.Quantity + dto.Quantity))
                throw new Exception("Estoque insuficiente.");
            existing.Quantity += dto.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = dto.ProductId, Quantity = dto.Quantity });
        }

        cart.LastUpdated = DateTime.UtcNow;
        await _uow.CommitAsync();
    }

    public async Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity)
    {
        if (quantity <= 0) { await RemoveItemAsync(userId, cartItemId); return; }

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null || item.Product == null) throw new Exception("Item inválido.");
        if (item.Product.StockQuantity < quantity) throw new Exception($"Estoque insuficiente.");

        item.Quantity = quantity;
        cart.LastUpdated = DateTime.UtcNow;
        await _uow.CommitAsync();
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