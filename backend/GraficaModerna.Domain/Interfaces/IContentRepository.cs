using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Domain.Interfaces;

public interface IContentRepository
{
    Task<ContentPage?> GetBySlugAsync(string slug);
    Task<IEnumerable<ContentPage>> GetAllAsync();
    Task AddAsync(ContentPage page);
    Task UpdateAsync(ContentPage page);
    Task<IEnumerable<SiteSetting>> GetSettingsAsync();
    Task UpdateSettingsAsync(IEnumerable<SiteSetting> settings);
}