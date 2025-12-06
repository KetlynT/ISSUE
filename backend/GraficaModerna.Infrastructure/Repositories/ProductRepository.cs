using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Adicionado para logging

namespace GraficaModerna.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger; // Logger injetado

    public ProductRepository(AppDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

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

            // 1. Filtro de Busca
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
            }

            // 2. Contagem Total
            var totalCount = await query.CountAsync();

            // 3. Ordenação Dinâmica
            query = sortColumn?.ToLower() switch
            {
                "price" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "name" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "stockquantity" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // 4. Paginação
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            // SEGURANÇA: Logamos o erro técnico (SQL query, timeout, etc)
            _logger.LogError(ex, "Erro ao recuperar produtos. Termo: {SearchTerm}, Page: {Page}", searchTerm, page);

            // SEGURANÇA: Lançamos uma exceção genérica para não expor a estrutura do DB
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
            // Evita expor constraints de banco de dados (ex: Unique Key violation)
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