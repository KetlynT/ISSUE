using GraficaModerna.Domain.Entities;

namespace GraficaModerna.Domain.Interfaces;

public interface IContentRepository
{
    Task<ContentPage?> GetBySlugAsync(string slug);
    Task AddAsync(ContentPage page);
    Task UpdateAsync(ContentPage page);
}