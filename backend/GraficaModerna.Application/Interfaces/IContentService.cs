using GraficaModerna.Application.DTOs;
using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Application.Interfaces;

public interface IContentService
{
    Task<ContentPage?> GetBySlugAsync(string slug);
    Task<IEnumerable<ContentPage>> GetAllPagesAsync();
    Task<ContentPage> CreateAsync(CreateContentDto dto);
    Task UpdateAsync(string slug, UpdateContentDto dto);
    Task<Dictionary<string, string>> GetSettingsAsync();
    Task UpdateSettingsAsync(Dictionary<string, string> settings);
}