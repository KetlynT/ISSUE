using Ganss.Xss;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContentController(IContentService service, IHtmlSanitizer sanitizer) : ControllerBase
{
    private readonly IHtmlSanitizer _sanitizer = sanitizer;
    private readonly IContentService _service = service; 

    [HttpGet("pages")]
    public async Task<IActionResult> GetAllPages()
    {
        var pages = await _service.GetAllPagesAsync();
        return Ok(pages);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _service.GetSettingsAsync();
        return Ok(settings);
    }

    [HttpPost("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, string> settings)
    {
        await _service.UpdateSettingsAsync(settings);
        return Ok();
    }

    [HttpPut("pages/{slug}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePage(string slug, [FromBody] UpdateContentDto dto)
    {
        dto.Content = _sanitizer.Sanitize(dto.Content);
        await _service.UpdateAsync(slug, dto);
        return Ok();
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPageExplicit(string slug)
    {
        return await GetPage(slug);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPage(string slug)
    {
        
        var page = await _service.GetBySlugAsync(slug);

        if (page == null)
            return NotFound();

        if (!string.IsNullOrEmpty(page.Content)) page.Content = _sanitizer.Sanitize(page.Content);

        return Ok(page);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("AdminPolicy")]
    public async Task<IActionResult> Create([FromBody] CreateContentDto dto)
    {
        dto.Content = _sanitizer.Sanitize(dto.Content);

        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }
}