using GraficaModerna.Domain.Models;
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
    Task<PagedResultDto<Order>> GetByUserIdAsync(string userId, int page, int pageSize);
    Task<PagedResultDto<Order>> GetAllAsync(int page, int pageSize);
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
