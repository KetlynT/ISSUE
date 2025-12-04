using GraficaModerna.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GraficaModerna.Infrastructure.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task BlacklistTokenAsync(string token, DateTime expiryDate)
    {
        var timeToLive = expiryDate - DateTime.UtcNow;

        // Se já expirou, não precisa guardar
        if (timeToLive <= TimeSpan.Zero) return Task.CompletedTask;

        _cache.Set(token, true, timeToLive);
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string token)
    {
        return Task.FromResult(_cache.TryGetValue(token, out _));
    }
}