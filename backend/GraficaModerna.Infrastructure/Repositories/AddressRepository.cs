using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _context;
    public AddressRepository(AppDbContext context) { _context = context; }

    public async Task<List<UserAddress>> GetByUserIdAsync(string userId)
    {
        return await _context.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync();
    }

    public async Task<UserAddress?> GetByIdAsync(Guid id, string userId)
    {
        return await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    }

    public async Task AddAsync(UserAddress address) => await _context.UserAddresses.AddAsync(address);

    public Task DeleteAsync(UserAddress address)
    {
        _context.UserAddresses.Remove(address);
        return Task.CompletedTask;
    }

    public async Task<bool> HasAnyAsync(string userId) => await _context.UserAddresses.AnyAsync(a => a.UserId == userId);
}