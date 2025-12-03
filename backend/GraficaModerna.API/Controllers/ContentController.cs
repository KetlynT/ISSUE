using GraficaModerna.Domain.Entities;
using GraficaModerna.Infrastructure.Context;
using Ganss.Xss; // Necessário: Pacote HtmlSanitizer
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHtmlSanitizer _sanitizer; // Injeção do Sanitizador

    public ContentController(AppDbContext context, IHtmlSanitizer sanitizer)
    {
        _context = context;
        _sanitizer = sanitizer;
    }

    [HttpGet("pages")]
    public async Task<IActionResult> GetAllPages()
    {
        return Ok(await _context.ContentPages
            .Select(p => new { p.Id, p.Slug, p.Title })
            .ToListAsync());
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPage(string slug)
    {
        var page = await _context.ContentPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page == null) return NotFound();
        return Ok(page);
    }

    [HttpPut("pages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] ContentPage model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var page = await _context.ContentPages.FindAsync(id);
        if (page == null) return NotFound();

        page.Title = model.Title;

        // SEGURANÇA (ENTRADA): Remove scripts e eventos perigosos (ex: onclick, <script>)
        // Isso impede que código malicioso seja sequer salvo no banco.
        page.Content = _sanitizer.Sanitize(model.Content);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.SiteSettings.ToListAsync();
        var dictionary = settings.ToDictionary(s => s.Key, s => s.Value);
        return Ok(dictionary);
    }

    [HttpPost("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> newSettings)
    {
        var dbSettings = await _context.SiteSettings.ToListAsync();

        foreach (var kvp in newSettings)
        {
            var setting = dbSettings.FirstOrDefault(s => s.Key == kvp.Key);

            // SEGURANÇA (ENTRADA): Sanitiza também as configurações do site
            // Isso previne XSS caso alguém tente injetar scripts no título ou rodapé
            var cleanValue = _sanitizer.Sanitize(kvp.Value);

            if (setting != null)
            {
                setting.UpdateValue(cleanValue);
            }
            else
            {
                _context.SiteSettings.Add(new SiteSetting(kvp.Key, cleanValue));
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}