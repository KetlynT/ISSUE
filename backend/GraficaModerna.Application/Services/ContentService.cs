using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;

namespace GraficaModerna.Application.Services;

public class ContentService(IContentRepository repository) : IContentService
{
    private readonly IContentRepository _repository = repository;

    public async Task<ContentPage?> GetBySlugAsync(string slug)
    {
        return await _repository.GetBySlugAsync(slug);
    }

    public async Task<IEnumerable<ContentPage>> GetAllPagesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<ContentPage> CreateAsync(CreateContentDto dto)
    {
        var page = new ContentPage
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Content = dto.Content,
            LastUpdated = DateTime.UtcNow
        };

        await _repository.AddAsync(page);
        return page;
    }

    public async Task UpdateAsync(string slug, UpdateContentDto dto)
    {
        var page = await _repository.GetBySlugAsync(slug) ?? throw new Exception("Página não encontrada.");
        page.Title = dto.Title;
        page.Content = dto.Content;
        page.LastUpdated = DateTime.UtcNow;

        await _repository.UpdateAsync(page);
    }

    public async Task<Dictionary<string, string>> GetSettingsAsync()
    {
        var settings = await _repository.GetSettingsAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task UpdateSettingsAsync(Dictionary<string, string> settingsDict)
    {
        var settingsList = settingsDict.Select(kvp => new SiteSetting(kvp.Key, kvp.Value)).ToList();
        await _repository.UpdateSettingsAsync(settingsList);
    }
}