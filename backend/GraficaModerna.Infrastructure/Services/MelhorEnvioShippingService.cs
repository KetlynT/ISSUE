using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GraficaModerna.Infrastructure.Services;

public class MelhorEnvioShippingService(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IWebHostEnvironment env,
    ILogger<MelhorEnvioShippingService> logger) : IShippingService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly AppDbContext _context = context;
    private readonly IWebHostEnvironment _env = env;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<MelhorEnvioShippingService> _logger = logger;

    public async Task<List<ShippingOptionDto>> CalculateAsync(string destinationCep, List<ShippingItemDto> items)
    {
    var token = Environment.GetEnvironmentVariable("MELHOR_ENVIO_TOKEN");

    if (string.IsNullOrEmpty(token) && _env.IsDevelopment()) token = _configuration["MelhorEnvio:Token"];

    if (string.IsNullOrEmpty(token))
    {
        _logger.LogWarning("Melhor Envio: Token não configurado. Cálculo ignorado.");
        return [];
    }

    if (items == null || items.Count == 0) return [];

    var originCep = "01001000";
    try
    {
        var originCepSetting = await _context.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "sender_cep");

        if (!string.IsNullOrEmpty(originCepSetting?.Value))
            originCep = originCepSetting.Value.Replace("-", "").Trim();
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Erro ao buscar CEP de origem no banco. Usando fallback.");
    }

    var requestPayload = new
    {
        from = new { postal_code = originCep },
        to = new { postal_code = destinationCep.Replace("-", "").Trim() },
        products = items.Select(i => new
        {
            width = i.Width,
            height = i.Height,
            length = i.Length,
            weight = i.Weight,
            insurance_value = 0,
            quantity = i.Quantity
        }).ToList()
    };

    try
    {
        var client = _httpClientFactory.CreateClient("MelhorEnvio");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "me/shipment/calculate");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var userAgent = _configuration["MelhorEnvio:UserAgent"] ?? "GraficaModernaAPI/1.0";
        requestMessage.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var jsonContent = JsonSerializer.Serialize(requestPayload, jsonOptions);
        requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        var response = await client.SendAsync(requestMessage, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
            _logger.LogError("Erro API Melhor Envio ({StatusCode}): {Body}", response.StatusCode, errorBody);
            return [];
        }

        var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
        var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioResponse>>(responseBody, jsonOptions);

        if (meOptions == null) return [];

        var validOptions = meOptions
            .Where(x => string.IsNullOrEmpty(x.Error))
            .Select(x =>
            {
                decimal price = 0;
                if (decimal.TryParse(x.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                    price = parsedPrice;
                else if (decimal.TryParse(x.CustomPrice, NumberStyles.Any, CultureInfo.InvariantCulture,
                             out var parsedCustomPrice)) price = parsedCustomPrice;

                return new ShippingOptionDto
                {
                    Name = $"{x.Company?.Name} - {x.Name}",
                    Price = price,
                    DeliveryDays = x.DeliveryRange?.Max ?? x.DeliveryTime,
                    Provider = "Melhor Envio"
                };
            })
            .Where(x => x.Price > 0)
            .OrderBy(x => x.Price)
            .ToList();

        return validOptions;
    }
    catch (OperationCanceledException)
    {
        _logger.LogError("Timeout na comunicação com Melhor Envio.");
        return [];
    }
    catch (Exception ex)
        {
        _logger.LogError(ex, "Erro crítico ao calcular frete Melhor Envio.");
        return [];
        }
    }
    private class MelhorEnvioResponse
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public string? Price { get; set; }
        [JsonPropertyName("custom_price")] public string? CustomPrice { get; set; }
        [JsonPropertyName("delivery_time")] public int DeliveryTime { get; set; }
        [JsonPropertyName("delivery_range")] public DeliveryRange? DeliveryRange { get; set; }
        [JsonPropertyName("company")] public CompanyObj? Company { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    private class DeliveryRange
    {
        [JsonPropertyName("max")] public int Max { get; set; }
        [JsonPropertyName("min")] public int Min { get; set; }
    }

    private class CompanyObj
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("picture")] public string? Picture { get; set; }
    }
}
