using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.Infrastructure.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly AppDbContext _context;

    public ContentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ContentPage?> GetBySlugAsync(string slug)
    {
        return await _context.ContentPages
            .FirstOrDefaultAsync(p => p.Slug == slug);
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
}