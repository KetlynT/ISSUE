using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace GraficaModerna.API.Middlewares;

public class JwtValidationMiddleware : IMiddleware
{
    private readonly ITokenBlacklistService _blacklistService;

    public JwtValidationMiddleware(ITokenBlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var token = ExtractToken(context);

        // Sem token ? deixa seguir apenas para endpoints anônimos
        if (string.IsNullOrEmpty(token))
        {
            await next(context);
            return;
        }

        var jwtHandler = new JwtSecurityTokenHandler();

        // Token malformado ? bloqueia
        if (!jwtHandler.CanReadToken(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token inválido.");
            return;
        }

        var jwt = jwtHandler.ReadJwtToken(token);

        // --- 1. Verificar se está na blacklist ---
        if (await _blacklistService.IsTokenBlacklistedAsync(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token revogado.");
            return;
        }

        // --- 2. Verificar expiração ---
        var exp = jwt.Payload.Exp;
        if (exp == null || DateTimeOffset.FromUnixTimeSeconds(exp.Value) < DateTimeOffset.UtcNow)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token expirado.");
            return;
        }

        // --- 3. Claims obrigatórias ---
        string[] requiredClaims = { "sub", "email", "role" };

        foreach (var claim in requiredClaims)
        {
            if (!jwt.Claims.Any(c => c.Type == claim))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync($"Token ausente da claim obrigatória: {claim}");
                return;
            }
        }

        await next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        // 1. Cookie HttpOnly
        if (context.Request.Cookies.TryGetValue("jwt", out var cookieToken))
            return cookieToken;

        // 2. Authorization: Bearer xxxx
        var header = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer "))
            return header.Substring("Bearer ".Length).Trim();

        return null;
    }
}
