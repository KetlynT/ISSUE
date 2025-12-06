using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq; // Necessário para o .Where(char.IsDigit)

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("ShippingPolicy")] // Proteção contra abuso (Rate Limiting)
public class ShippingController : ControllerBase
{
    private readonly IEnumerable<IShippingService> _shippingServices;
    private readonly IProductService _productService;
    private readonly ILogger<ShippingController> _logger; // Injeção do Logger para segurança
    private const int MaxItemsPerCalculation = 50;

    public ShippingController(
        IEnumerable<IShippingService> shippingServices,
        IProductService productService,
        ILogger<ShippingController> logger)
    {
        _shippingServices = shippingServices;
        _productService = productService;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<List<ShippingOptionDto>>> Calculate([FromBody] CalculateShippingRequest request)
    {
        // 1. VALIDAÇÃO BÁSICA
        if (request == null || string.IsNullOrWhiteSpace(request.DestinationCep))
            return BadRequest(new { message = "CEP de destino inválido." });

        // 2. SANITIZAÇÃO DE INPUT (Segurança)
        // Remove qualquer caractere que não seja número (hífens, espaços, letras)
        var cleanCep = new string(request.DestinationCep.Where(char.IsDigit).ToArray());

        if (cleanCep.Length != 8)
            return BadRequest(new { message = "CEP inválido. Certifique-se de informar os 8 dígitos numéricos." });

        // 3. VALIDAÇÃO DE ITENS
        if (request.Items == null || !request.Items.Any())
            return BadRequest(new { message = "Nenhum item informado para cálculo." });

        if (request.Items.Count > MaxItemsPerCalculation)
            return BadRequest(new { message = $"O cálculo é limitado a {MaxItemsPerCalculation} itens distintos por vez." });

        var validatedItems = new List<ShippingItemDto>();

        // 4. PREPARAÇÃO DOS DADOS (Busca detalhes seguros do produto no banco)
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

        if (!validatedItems.Any())
            return BadRequest(new { message = "Nenhum produto válido encontrado para cálculo." });

        var allOptions = new List<ShippingOptionDto>();

        try
        {
            // 5. CÁLCULO EXTERNO
            // Utiliza o 'cleanCep' sanitizado
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
            // CORREÇÃO DE SEGURANÇA:
            // Logamos o erro técnico com StackTrace no servidor
            _logger.LogError(ex, "Erro crítico ao calcular frete para CEP {Cep}", cleanCep);

            // Retornamos apenas uma mensagem amigável para o cliente (sem ex.Message)
            return StatusCode(500, new { message = "Não foi possível calcular o frete no momento. Tente novamente mais tarde." });
        }
    }

    [HttpGet("product/{productId}/{cep}")]
    public async Task<ActionResult<List<ShippingOptionDto>>> CalculateForProduct(Guid productId, string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return BadRequest(new { message = "CEP inválido." });

        // Sanitização
        var cleanCep = new string(cep.Where(char.IsDigit).ToArray());

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

            var allOptions = new List<ShippingOptionDto>();

            var tasks = _shippingServices.Select(s => s.CalculateAsync(cleanCep, new List<ShippingItemDto> { item }));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results) allOptions.AddRange(result);

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            // CORREÇÃO DE SEGURANÇA
            _logger.LogError(ex, "Erro ao calcular frete único para Produto {ProductId} e CEP {Cep}", productId, cleanCep);

            return StatusCode(500, new { message = "Serviço de cálculo de frete indisponível temporariamente." });
        }
    }
}