using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("ShippingPolicy")]
// CORREÇÃO IDE0290: Construtor Primário
public class ShippingController(
    IEnumerable<IShippingService> shippingServices,
    IProductService productService,
    ILogger<ShippingController> logger) : ControllerBase
{
    private readonly IEnumerable<IShippingService> _shippingServices = shippingServices;
    private readonly IProductService _productService = productService;
    private readonly ILogger<ShippingController> _logger = logger;
    private const int MaxItemsPerCalculation = 50;

    [HttpPost("calculate")]
    public async Task<ActionResult<List<ShippingOptionDto>>> Calculate([FromBody] CalculateShippingRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DestinationCep))
            return BadRequest(new { message = "CEP de destino inválido." });

        var cleanCep = new string([.. request.DestinationCep.Where(char.IsDigit)]);

        if (cleanCep.Length != 8)
            return BadRequest(new { message = "CEP inválido. Certifique-se de informar os 8 dígitos numéricos." });

        // CORREÇÃO CA1860: Usar Count ou Length em vez de Any()
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "Nenhum item informado para cálculo." });

        if (request.Items.Count > MaxItemsPerCalculation)
            return BadRequest(new { message = $"O cálculo é limitado a {MaxItemsPerCalculation} itens distintos por vez." });

        // CORREÇÃO IDE0028: Inicialização simplificada
        List<ShippingItemDto> validatedItems = [];

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(new { message = $"Item {item.ProductId} possui quantidade inválida ({item.Quantity})." });

            if (item.Quantity > 1000)
                return BadRequest(new { message = $"Quantidade excessiva para o item {item.ProductId}. Entre em contato para cotação de atacado." });

            if (item.ProductId != Guid.Empty)
            {
                var product = await _productService.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    validatedItems.Add(new ShippingItemDto
                    {
                        ProductId = product.Id,
                        Weight = product.Weight,
                        Width = product.Width,
                        Height = product.Height,
                        Length = product.Length,
                        Quantity = item.Quantity
                    });
                }
            }
        }

        // CORREÇÃO CA1860: Count == 0
        if (validatedItems.Count == 0)
            return BadRequest(new { message = "Nenhum produto válido encontrado para cálculo." });

        // CORREÇÃO IDE0028: Inicialização simplificada
        List<ShippingOptionDto> allOptions = [];

        try
        {
            var tasks = _shippingServices.Select(service => service.CalculateAsync(cleanCep, validatedItems));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                allOptions.AddRange(result);
            }

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao calcular frete para CEP {Cep}", cleanCep);
            return StatusCode(500, new { message = "Não foi possível calcular o frete no momento. Tente novamente mais tarde." });
        }
    }

    [HttpGet("product/{productId}/{cep}")]
    public async Task<ActionResult<List<ShippingOptionDto>>> CalculateForProduct(Guid productId, string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return BadRequest(new { message = "CEP inválido." });

        var cleanCep = new string([.. cep.Where(char.IsDigit)]);

        if (cleanCep.Length != 8)
            return BadRequest(new { message = "CEP inválido. Informe apenas os 8 dígitos." });

        try
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product == null) return NotFound(new { message = "Produto não encontrado." });

            var item = new ShippingItemDto
            {
                ProductId = product.Id,
                Weight = product.Weight,
                Height = product.Height,
                Width = product.Width,
                Length = product.Length,
                Quantity = 1
            };

            // CORREÇÃO IDE0028/IDE0305: Uso de [] para criar lista
            List<ShippingOptionDto> allOptions = [];

            // CORREÇÃO IDE0305: Passando lista simplificada no parâmetro
            var tasks = _shippingServices.Select(s => s.CalculateAsync(cleanCep, [item]));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results) allOptions.AddRange(result);

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular frete único para Produto {ProductId} e CEP {Cep}", productId, cleanCep);
            return StatusCode(500, new { message = "Serviço de cálculo de frete indisponível temporariamente." });
        }
    }
}