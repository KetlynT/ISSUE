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
        // 1. Pega configurações
        var baseUrl = _configuration["MelhorEnvio:BaseUrl"];
        var token = _configuration["MelhorEnvio:Token"];
        var userAgent = _configuration["MelhorEnvio:UserAgent"];

        // 2. Pega CEP de Origem do Banco
        var originCepSetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "sender_cep");
        string originCep = originCepSetting?.Value?.Replace("-", "") ?? "01001000"; // Fallback SP

        // 3. Monta o Payload do Melhor Envio
        var requestPayload = new
        {
            from = new { postal_code = originCep },
            to = new { postal_code = destinationCep.Replace("-", "") },
            products = items.Select(i => new
            {
                width = i.Width,
                height = i.Height,
                length = i.Length,
                weight = i.Weight, // Melhor Envio aceita peso em KG (ex: 0.5)
                insurance_value = 0, // Opcional: poderia vir do preço do produto
                quantity = i.Quantity
            }).ToList()
        };

        // 4. Configura a Requisição
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v2/me/shipment/calculate");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var jsonContent = JsonSerializer.Serialize(requestPayload, jsonOptions);
        requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            // 5. Envia
            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                // Logar o erro aqui se necessário
                // var errorBody = await response.Content.ReadAsStringAsync();
                return new List<ShippingOptionDto>();
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            // 6. Deserializa e Mapeia para o DTO do seu sistema
            var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioResponse>>(responseBody, jsonOptions);

            if (meOptions == null) return new List<ShippingOptionDto>();

            // Filtra opções com erro e mapeia
            return meOptions
                .Where(x => string.IsNullOrEmpty(x.Error))
                .Select(x => new ShippingOptionDto
                {
                    Name = $"{x.Company?.Name} {x.Name}", // Ex: "Correios SEDEX" ou "Jadlog .Package"
                    Provider = "Melhor Envio",
                    Price = decimal.Parse(x.Price ?? "0", System.Globalization.CultureInfo.InvariantCulture), // Melhor envio pode mandar string "25.50"
                    DeliveryDays = x.DeliveryRange?.Max ?? x.DeliveryTime // Pega o prazo máximo
                })
                .OrderBy(x => x.Price)
                .ToList();
        }
        catch (Exception)
        {
            // Em caso de falha de conexão ou timeout
            return new List<ShippingOptionDto>();
        }
    }

    // Classes auxiliares para deserialização (internas)
    private class MelhorEnvioResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("delivery_time")]
        public int DeliveryTime { get; set; }

        [JsonPropertyName("delivery_range")]
        public DeliveryRange? DeliveryRange { get; set; }

        [JsonPropertyName("company")]
        public CompanyObj? Company { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private class DeliveryRange
    {
        [JsonPropertyName("max")]
        public int Max { get; set; }
    }

    private class CompanyObj
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}