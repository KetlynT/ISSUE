using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Repositories;

public class ContentRepository(AppDbContext context) : IContentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<ContentPage?> GetBySlugAsync(string slug)
    {
        return await _context.ContentPages
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<ContentPage>> GetAllAsync()
    {
        return await _context.ContentPages.ToListAsync();
    }

    public async Task AddAsync(ContentPage page)
    {
        _context.ContentPages.Add(page);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ContentPage page)
    {
        _context.ContentPages.Update(page);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SiteSetting>> GetSettingsAsync()
    {
        return await _context.SiteSettings.ToListAsync();
    }

    public async Task UpdateSettingsAsync(IEnumerable<SiteSetting> settings)
    {
        foreach (var setting in settings)
        {
            var existing = await _context.SiteSettings.FindAsync(setting.Key);
            if (existing != null)
            {
                existing.UpdateValue(setting.Value);
            }
            else
            {
                await _context.SiteSettings.AddAsync(setting);
            }
        }
        await _context.SaveChangesAsync();
    }
}