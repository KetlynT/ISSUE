using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GraficaModerna.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context, ILogger<ProductRepository> logger) : IProductRepository
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<ProductRepository> _logger = logger; 

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(
        string? searchTerm,
        string? sortColumn,
        string? sortOrder,
        int page,
        int pageSize)
    {
        try
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                
                var sanitizedTerm = term
                .Replace("\\", "\\\\") 
                .Replace("%", "\\%")
                .Replace("_", "\\_");

                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{sanitizedTerm}%") ||
                    EF.Functions.ILike(p.Description, $"%{sanitizedTerm}%"));
            }

            var totalCount = await query.CountAsync();

            var allowedSortColumns = new[] { "price", "name", "stockquantity" };
            var safeSortColumn = sortColumn?.ToLower();
            
            if (!string.IsNullOrEmpty(safeSortColumn) && 
                !allowedSortColumns.Contains(safeSortColumn))
            {
                _logger.LogWarning(
                    "Tentativa de ordenação por coluna não permitida: {Column}", sortColumn);
                safeSortColumn = null; 
            }

            query = safeSortColumn switch
            {
                "price" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),
                "name" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                "stockquantity" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.StockQuantity)
                    : query.OrderBy(p => p.StockQuantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar produtos. Page: {Page}, PageSize: {PageSize}", page, pageSize);
            throw new Exception("Não foi possível recuperar a lista de produtos no momento.");
        }
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto por ID: {ProductId}", id);
            throw new Exception("Erro ao buscar detalhes do produto.");
        }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        try
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto: {ProductName}", product.Name);
            throw new Exception("Erro interno ao salvar o produto.");
        }
    }

    public async Task UpdateAsync(Product product)
    {
        try
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto ID: {ProductId}", product.Id);
            throw new Exception("Erro interno ao atualizar o produto.");
        }
    }
}