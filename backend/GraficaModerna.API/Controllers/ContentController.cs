using Ganss.Xss; // Certifique-se de que este using está presente
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces; // Supondo que exista um IContentService ou Repository
using Microsoft.AspNetCore.Mvc;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContentController : ControllerBase
{
    private readonly IContentService _service; // Ou IContentRepository
    private readonly IHtmlSanitizer _sanitizer;

    // Injeção de dependência do Sanitizer (configurado no Program.cs)
    public ContentController(IContentService service, IHtmlSanitizer sanitizer)
    {
        _service = service;
        _sanitizer = sanitizer;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPage(string slug)
    {
        var page = await _service.GetBySlugAsync(slug);

        if (page == null)
            return NotFound();

        // ==============================================================================
        // CORREÇÃO DE SEGURANÇA (Output Sanitization)
        // Mesmo que o dado tenha sido limpo na entrada, limpamos novamente na saída.
        // Isso protege contra "Stored XSS" caso o banco tenha sido comprometido.
        // ==============================================================================
        if (!string.IsNullOrEmpty(page.Content))
        {
            page.Content = _sanitizer.Sanitize(page.Content);
        }

        return Ok(page);
    }

    // O método de criação/edição (POST/PUT) deve continuar sanitizando a entrada
    // para evitar "lixo" no banco, mas a segurança real vem do GET acima.
    [HttpPost]
    // [Authorize(Roles = "Admin")] // Importante restringir quem cria conteúdo
    public async Task<IActionResult> Create([FromBody] CreateContentDto dto)
    {
        // Sanitização de Entrada (Input Sanitization) - Mantém o banco limpo
        dto.Content = _sanitizer.Sanitize(dto.Content);

        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }
}