using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("ShippingPolicy")]
public class ShippingController(
    IEnumerable<IShippingService> shippingServices,
    IProductService productService,
    IContentService contentService,
    ILogger<ShippingController> logger) : ControllerBase
{
    private const int MaxItemsPerCalculation = 50;
    private readonly ILogger<ShippingController> _logger = logger;
    private readonly IProductService _productService = productService;
    private readonly IEnumerable<IShippingService> _shippingServices = shippingServices;
    private readonly IContentService _contentService = contentService;

    private async Task CheckPurchaseEnabled()
    {
        var settings = await _contentService.GetSettingsAsync();
        if (settings.TryGetValue("purchase_enabled", out var enabled) && enabled == "false")
            throw new Exception("Cálculo de frete temporariamente indisponível. Utilize o orçamento personalizado.");
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<List<ShippingOptionDto>>> Calculate([FromBody] CalculateShippingRequest request)
    {
        try
        {
            await CheckPurchaseEnabled();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.DestinationCep))
            return BadRequest(new { message = "CEP de destino inválido." });

        var cleanCep = new string([.. request.DestinationCep.Where(char.IsDigit)]);

        if (cleanCep.Length != 8)
            return BadRequest(new { message = "CEP inválido. Certifique-se de informar os 8 dígitos numéricos." });

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "Nenhum item informado para cálculo." });

        if (request.Items.Count > MaxItemsPerCalculation)
            return BadRequest(new
                { message = $"O cálculo é limitado a {MaxItemsPerCalculation} itens distintos por vez." });

        List<ShippingItemDto> validatedItems = [];

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(new
                    { message = $"Item {item.ProductId} possui quantidade inválida ({item.Quantity})." });

            if (item.Quantity > 1000)
                return BadRequest(new
                {
                    message =
                        $"Quantidade excessiva para o item {item.ProductId}. Entre em contato para cotação de atacado."
                });

            if (item.ProductId != Guid.Empty)
            {
                var product = await _productService.GetByIdAsync(item.ProductId);
                if (product != null)
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

        if (validatedItems.Count == 0)
            return BadRequest(new { message = "Nenhum produto válido encontrado para cálculo." });

        List<ShippingOptionDto> allOptions = [];

        try
        {
            var tasks = _shippingServices.Select(service => service.CalculateAsync(cleanCep, validatedItems));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results) allOptions.AddRange(result);

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao calcular frete para CEP {Cep}", cleanCep);
            return StatusCode(500,
                new { message = "Não foi possível calcular o frete no momento. Tente novamente mais tarde." });
        }
    }

    [HttpGet("product/{productId}/{cep}")]
    public async Task<ActionResult<List<ShippingOptionDto>>> CalculateForProduct(Guid productId, string cep)
    {
        try
        {
            await CheckPurchaseEnabled();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

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

            List<ShippingOptionDto> allOptions = [];

            var tasks = _shippingServices.Select(s => s.CalculateAsync(cleanCep, [item]));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results) allOptions.AddRange(result);

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular frete único para Produto {ProductId} e CEP {Cep}", productId,
                cleanCep);
            return StatusCode(500, new { message = "Serviço de cálculo de frete indisponível temporariamente." });
        }
    }
}