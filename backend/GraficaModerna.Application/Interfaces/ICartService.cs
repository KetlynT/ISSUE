using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task AddItemAsync(string userId, AddToCartDto dto);
    Task UpdateItemQuantityAsync(string userId, Guid cartItemId, int quantity); // NOVO MÉTODO
    Task RemoveItemAsync(string userId, Guid cartItemId);
    Task ClearCartAsync(string userId);
}