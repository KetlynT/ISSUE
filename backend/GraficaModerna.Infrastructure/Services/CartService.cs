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

        // Proteção contra produtos que podem ter sido deletados do DB mas ainda estarem no carrinho
        var itemsDto = cart.Items
            .Where(i => i.Product != null)
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

        // Se encontrou itens órfãos (Product == null), limpa-os silenciosamente
        if (cart.Items.Any(i => i.Product == null))
        {
            // Opcional: Implementar limpeza assíncrona
        }

        return new CartDto(cart.Id, itemsDto, itemsDto.Sum(i => i.TotalPrice));
    }

    public async Task AddItemAsync(string userId, AddToCartDto dto)
    {
        var product = await _uow.Products.GetByIdAsync(dto.ProductId);
        if (product == null || !product.IsActive)
            throw new Exception("Produto indisponível ou removido.");

        if (product.StockQuantity < dto.Quantity)
            throw new Exception($"Estoque insuficiente. Restam apenas {product.StockQuantity} unidades.");

        var cart = await GetOrCreateCart(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existing != null)
        {
            if (product.StockQuantity < (existing.Quantity + dto.Quantity))
                throw new Exception($"Não é possível adicionar mais itens. O estoque total é {product.StockQuantity}.");

            existing.Quantity += dto.Quantity;
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
        await _uow.CommitAsync();
    }

    public async Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveItemAsync(userId, cartItemId);
            return;
        }

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null) throw new Exception("Item não encontrado no carrinho.");

        // Carrega produto para validar estoque novamente
        var product = await _uow.Products.GetByIdAsync(item.ProductId);
        if (product == null || !product.IsActive) throw new Exception("Produto indisponível.");

        if (product.StockQuantity < quantity)
            throw new Exception($"Estoque insuficiente para a quantidade solicitada.");

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