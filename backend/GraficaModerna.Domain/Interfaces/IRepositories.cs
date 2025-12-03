using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId);
    Task AddAsync(Cart cart);
    Task RemoveItemAsync(CartItem item);
    Task ClearCartAsync(Guid cartId);
}

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetByUserIdAsync(string userId);
    Task<List<Order>> GetAllAsync();
    Task UpdateAsync(Order order);
}

public interface IAddressRepository
{
    Task<List<UserAddress>> GetByUserIdAsync(string userId);
    Task<UserAddress?> GetByIdAsync(Guid id, string userId);
    Task AddAsync(UserAddress address);
    Task DeleteAsync(UserAddress address);
    Task<bool> HasAnyAsync(string userId);
}

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code);
    Task<List<Coupon>> GetAllAsync();
    Task AddAsync(Coupon coupon);
    Task DeleteAsync(Guid id);
}