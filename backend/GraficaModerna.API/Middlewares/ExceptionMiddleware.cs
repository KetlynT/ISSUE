using System.Net;
using System.Text.Json;

namespace GraficaModerna.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // SEGURANÇA: O TraceId permite correlacionar o erro no log sem expor dados ao usuário
            var traceId = context.TraceIdentifier;

            _logger.LogError(ex, "Um erro não tratado ocorreu. TraceId: {TraceId}", traceId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                // Em Produção, ocultamos a mensagem original que pode conter dados sensíveis (connection strings, paths, etc)
                Message = _env.IsDevelopment() ? ex.Message : "Erro interno no servidor. Contate o suporte informando o código de rastreio.",
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : string.Empty,
                TraceId = traceId
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}