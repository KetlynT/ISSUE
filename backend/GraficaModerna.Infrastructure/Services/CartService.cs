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
    private const int MaxConcurrencyRetries = 3;

    public CartService(IUnitOfWork uow, AppDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    private async Task<Cart> GetOrCreateCart(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId inválido.", nameof(userId));

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

        // Proteção contra produtos deletados/inativos
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
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId inválido.", nameof(userId));
        if (dto.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(dto.Quantity), "Quantidade deve ser maior que zero.");

        for (int attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Consulta via LINQ (sem SQL cru) para evitar injeção e manter portabilidade
                var product = await _context.Products
                    .Where(p => p.Id == dto.ProductId && p.IsActive)
                    .SingleOrDefaultAsync();

                if (product == null)
                {
                    throw new InvalidOperationException("Produto indisponível ou removido.");
                }

                if (product.StockQuantity < dto.Quantity)
                {
                    throw new InvalidOperationException("Estoque insuficiente para a quantidade solicitada.");
                }

                var cart = await GetOrCreateCart(userId);
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

                if (existing != null)
                {
                    int newTotal = existing.Quantity + dto.Quantity;
                    if (product.StockQuantity < newTotal)
                    {
                        throw new InvalidOperationException("Não é possível adicionar mais itens: estoque insuficiente.");
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

                // Persistir usando UnitOfWork para manter consistência
                await _uow.CommitAsync();

                await transaction.CommitAsync();
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (attempt == MaxConcurrencyRetries)
                    throw;
                // retry
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId inválido.", nameof(userId));
        if (quantity <= 0)
        {
            await RemoveItemAsync(userId, cartItemId);
            return;
        }

        for (int attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await GetOrCreateCart(userId);
                var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

                if (item == null)
                {
                    throw new InvalidOperationException("Item não encontrado no carrinho.");
                }

                var product = await _context.Products
                    .Where(p => p.Id == item.ProductId && p.IsActive)
                    .SingleOrDefaultAsync();

                if (product == null)
                {
                    throw new InvalidOperationException("Produto indisponível.");
                }

                if (product.StockQuantity < quantity)
                {
                    throw new InvalidOperationException("Estoque insuficiente para a quantidade solicitada.");
                }

                item.Quantity = quantity;
                cart.LastUpdated = DateTime.UtcNow;

                await _uow.CommitAsync();
                await transaction.CommitAsync();
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                if (attempt == MaxConcurrencyRetries)
                    throw;
                // retry
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task RemoveItemAsync(string userId, Guid itemId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId inválido.", nameof(userId));

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
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId inválido.", nameof(userId));

        var cart = await _uow.Carts.GetByUserIdAsync(userId);
        if (cart != null)
        {
            await _uow.Carts.ClearCartAsync(cart.Id);
            await _uow.CommitAsync();
        }
    }
}