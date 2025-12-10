using GraficaModerna.Domain.Models;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Repositories;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User) // Inclui dados do usuário
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<PagedResultDto<Order>> GetByUserIdAsync(string userId, int page, int pageSize)
{
    var query = _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User)
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.OrderDate);

    var totalItems = await query.CountAsync();

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResultDto<Order>
    {
        Items = items,
        TotalItems = totalItems,
        Page = page,
        PageSize = pageSize
    };
}

public async Task<PagedResultDto<Order>> GetAllAsync(int page, int pageSize)
{
    var query = _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User)
        .OrderByDescending(o => o.OrderDate);

    var totalItems = await query.CountAsync();

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResultDto<Order>
    {
        Items = items,
        TotalItems = totalItems,
        Page = page,
        PageSize = pageSize
    };
}

    public Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }
}