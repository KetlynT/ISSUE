using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public MelhorEnvioShippingService(AppDbContext context, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<ShippingOptionDto>> CalculateAsync(string destinationCep, List<ShippingItemDto> items)
    {
        var baseUrl = _configuration["MelhorEnvio:BaseUrl"];
        var userAgent = _configuration["MelhorEnvio:UserAgent"];

        // SEGURANÇA: O Token JAMAIS deve vir do appsettings em produção.
        // Ele deve ser injetado via Environment Variable do servidor (ex: Azure App Service, Docker, AWS).
        var token = Environment.GetEnvironmentVariable("MELHOR_ENVIO_TOKEN");

        // Fallback apenas para desenvolvimento local se necessário, mas desencorajado.
        if (string.IsNullOrEmpty(token))
        {
            // Log de aviso: Token não encontrado nas variáveis de ambiente
            return new List<ShippingOptionDto>();
        }

        var originCepSetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "sender_cep");
        string originCep = originCepSetting?.Value?.Replace("-", "") ?? "01001000";

        var requestPayload = new
        {
            from = new { postal_code = originCep },
            to = new { postal_code = destinationCep.Replace("-", "") },
            products = items.Select(i => new
            {
                // SEGURANÇA: O Controller já validou que esses dados vieram do banco de dados,
                // e não do input do usuário, prevenindo Parameter Tampering.
                width = i.Width,
                height = i.Height,
                length = i.Length,
                weight = i.Weight,
                insurance_value = 0,
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

            if (!response.IsSuccessStatusCode)
            {
                return new List<ShippingOptionDto>();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioResponse>>(responseBody, jsonOptions);

            if (meOptions == null) return new List<ShippingOptionDto>();

            return meOptions
                .Where(x => string.IsNullOrEmpty(x.Error))
                .Select(x => new ShippingOptionDto
                {
                    Name = $"{x.Company?.Name} {x.Name}",
                    Provider = "Melhor Envio",
                    Price = decimal.Parse(x.Price ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                    DeliveryDays = x.DeliveryRange?.Max ?? x.DeliveryTime
                })
                .OrderBy(x => x.Price)
                .ToList();
        }
        catch (Exception)
        {
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