using System.Net;
using System.Text.Json;

namespace GraficaModerna.API.Middlewares;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;

            logger.LogError(ex,
                "Um erro não tratado ocorreu. TraceId: {TraceId}",
                traceId);

            context.Response.ContentType = "application/json";

            context.Response.StatusCode = ex switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.Headers["X-Trace-Id"] = traceId;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = env.IsDevelopment() ? ex.Message : "Erro interno no servidor.",
                stackTrace = env.IsDevelopment() ? ex.StackTrace : null,
                traceId
            };

            var json = JsonSerializer.Serialize(response, _jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
