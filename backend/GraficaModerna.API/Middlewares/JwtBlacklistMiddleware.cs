using System.IdentityModel.Tokens.Jwt;
using GraficaModerna.Application.Interfaces;

namespace GraficaModerna.API.Middlewares;

public class JwtValidationMiddleware(ITokenBlacklistService blacklistService) : IMiddleware
{
    private readonly ITokenBlacklistService _blacklistService = blacklistService;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var token = ExtractToken(context);

        if (string.IsNullOrEmpty(token))
        {
            await next(context);
            return;
        }

        if (!await IsTokenValidAsync(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private async Task<bool> IsTokenValidAsync(string token)
    {
        var jwtHandler = new JwtSecurityTokenHandler();

        if (!jwtHandler.CanReadToken(token))
        {
            return false;
        }

        if (await _blacklistService.IsTokenBlacklistedAsync(token))
        {
            return false;
        }

        try
        {
            var jwt = jwtHandler.ReadJwtToken(token);

            var exp = jwt.Payload.Expiration;
            if (exp == null || DateTimeOffset.FromUnixTimeSeconds(exp.Value) < DateTimeOffset.UtcNow)
            {
                return false;
            }

            var hasSubject = jwt.Claims.Any(c => c.Type == "sub" || c.Type == "nameid");
            var hasEmail = jwt.Claims.Any(c => c.Type == "email");
            var hasRole = jwt.Claims.Any(c => c.Type == "role");

            if (!hasSubject || !hasEmail || !hasRole)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer "))
            return header["Bearer ".Length..].Trim();

        if (context.Request.Cookies.TryGetValue("jwt", out var cookieToken))
            return cookieToken;

        return null;
    }
}