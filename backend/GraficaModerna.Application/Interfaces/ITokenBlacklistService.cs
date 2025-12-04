namespace GraficaModerna.Application.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token, DateTime expiryDate);
    Task<bool> IsTokenBlacklistedAsync(string token);
}