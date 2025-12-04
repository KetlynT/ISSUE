using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraficaModerna.Infrastructure.Services;

public class MelhorEnvioShippingService : IShippingService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MelhorEnvioShippingService> _logger;

    public MelhorEnvioShippingService(
        AppDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<MelhorEnvioShippingService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    public async Task<List<ShippingOptionDto>> CalculateAsync(string destinationCep, List<ShippingItemDto> items)
    {
        var baseUrl = _configuration["MelhorEnvio:BaseUrl"];
        var userAgent = _configuration["MelhorEnvio:UserAgent"] ?? "GraficaModerna/1.0 (contato@graficamoderna.com)";

        // 1. Lógica de Segurança para o Token
        string? token = Environment.GetEnvironmentVariable("MELHOR_ENVIO_TOKEN");

        // Em DEV, permitimos fallback para o appsettings para facilitar testes
        if (string.IsNullOrEmpty(token) && _env.IsDevelopment())
        {
            token = _configuration["MelhorEnvio:Token"];
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Melhor Envio: Token não configurado (Env Var: MELHOR_ENVIO_TOKEN).");
            return new List<ShippingOptionDto>();
        }

        // Validação de CEP de Origem
        var originCepSetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "sender_cep");
        string originCep = originCepSetting?.Value?.Replace("-", "") ?? "01001000"; // Fallback para Sé, SP

        if (items == null || !items.Any()) return new List<ShippingOptionDto>();

        var requestPayload = new
        {
            from = new { postal_code = originCep },
            to = new { postal_code = destinationCep.Replace("-", "") },
            products = items.Select(i => new
            {
                width = i.Width,
                height = i.Height,
                length = i.Length,
                weight = i.Weight,
                insurance_value = 0, // Pode ajustar conforme regra de negócio
                quantity = i.Quantity
            }).ToList()
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v2/me/shipment/calculate");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var jsonContent = JsonSerializer.Serialize(requestPayload, jsonOptions);
        requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Erro API Melhor Envio ({response.StatusCode}): {responseBody}");
                return new List<ShippingOptionDto>();
            }

            // Tratamento de resposta
            var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioResponse>>(responseBody, jsonOptions);

            if (meOptions == null) return new List<ShippingOptionDto>();

            // Filtra opções com erro e converte
            var validOptions = meOptions
                .Where(x => string.IsNullOrEmpty(x.Error))
                .Select(x => {
                    decimal.TryParse(x.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price);
                    return new ShippingOptionDto
                    {
                        Name = $"{x.Company?.Name} {x.Name}",
                        Provider = "Melhor Envio",
                        Price = price,
                        DeliveryDays = x.DeliveryRange?.Max ?? x.DeliveryTime
                    };
                })
                .OrderBy(x => x.Price)
                .ToList();

            return validOptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao calcular frete.");
            return new List<ShippingOptionDto>();
        }
    }

    private class MelhorEnvioResponse
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public string? Price { get; set; }
        [JsonPropertyName("delivery_time")] public int DeliveryTime { get; set; }
        [JsonPropertyName("delivery_range")] public DeliveryRange? DeliveryRange { get; set; }
        [JsonPropertyName("company")] public CompanyObj? Company { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    private class DeliveryRange { [JsonPropertyName("max")] public int Max { get; set; } }
    private class CompanyObj { [JsonPropertyName("name")] public string? Name { get; set; } }
}