using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq; // Necessário para o .Where(char.IsDigit)

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("ShippingPolicy")]
public class ShippingController : ControllerBase
{
    private readonly IEnumerable<IShippingService> _shippingServices;
    private readonly IProductService _productService;
    private const int MaxItemsPerCalculation = 50;

    public ShippingController(IEnumerable<IShippingService> shippingServices, IProductService productService)
    {
        _shippingServices = shippingServices;
        _productService = productService;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<List<ShippingOptionDto>>> Calculate([FromBody] CalculateShippingRequest request)
    {
        // VALIDAÇÃO: Input nulo ou vazio
        if (request == null || string.IsNullOrWhiteSpace(request.DestinationCep))
            return BadRequest("CEP de destino inválido.");

        // CORREÇÃO: Sanitização e Validação Estrita de CEP
        // Remove qualquer caractere que não seja número (hífens, espaços, letras)
        var cleanCep = new string(request.DestinationCep.Where(char.IsDigit).ToArray());

        if (cleanCep.Length != 8)
            return BadRequest("CEP inválido. Certifique-se de informar os 8 dígitos numéricos.");

        if (request.Items == null || !request.Items.Any())
            return BadRequest("Nenhum item informado para cálculo.");

        if (request.Items.Count > MaxItemsPerCalculation)
            return BadRequest($"O cálculo é limitado a {MaxItemsPerCalculation} itens distintos por vez.");

        var validatedItems = new List<ShippingItemDto>();

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest($"Item {item.ProductId} possui quantidade inválida ({item.Quantity}).");

            if (item.Quantity > 1000)
                return BadRequest($"Quantidade excessiva para o item {item.ProductId}. Entre em contato para cotação de atacado.");

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
            return BadRequest("Nenhum produto válido encontrado para cálculo.");

        var allOptions = new List<ShippingOptionDto>();

        // CORREÇÃO: Usamos 'cleanCep' sanitizado ao invés do input bruto do usuário
        var tasks = _shippingServices.Select(service => service.CalculateAsync(cleanCep, validatedItems));

        try
        {
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                allOptions.AddRange(result);
            }

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            // Logar 'ex' internamente se possível
            return StatusCode(500, $"Erro ao calcular frete: {ex.Message}");
        }
    }

    [HttpGet("product/{productId}/{cep}")]
    public async Task<ActionResult<List<ShippingOptionDto>>> CalculateForProduct(Guid productId, string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return BadRequest("CEP inválido.");

        // CORREÇÃO: Sanitização também no GET
        var cleanCep = new string(cep.Where(char.IsDigit).ToArray());

        if (cleanCep.Length != 8)
            return BadRequest("CEP inválido. Informe apenas os 8 dígitos.");

        var product = await _productService.GetByIdAsync(productId);
        if (product == null) return NotFound("Produto não encontrado.");

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

        // CORREÇÃO: Usar cleanCep
        var tasks = _shippingServices.Select(s => s.CalculateAsync(cleanCep, new List<ShippingItemDto> { item }));

        try
        {
            var results = await Task.WhenAll(tasks);
            foreach (var result in results) allOptions.AddRange(result);

            return Ok(allOptions.OrderBy(x => x.Price));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}