using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GraficaModerna.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;

    public ProductService(IProductRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PagedResultDto<ProductResponseDto>> GetCatalogAsync(string? search, string? sort, string? order, int page, int pageSize)
    {
        var cacheKey = $"catalog_{search}_{sort}_{order}_{page}_{pageSize}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);

            var (products, totalCount) = await _repository.GetAllAsync(search, sort, order, page, pageSize);

            return new PagedResultDto<ProductResponseDto>
            {
                Items = products.Select(MapToDto).ToList(),
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }) ?? new PagedResultDto<ProductResponseDto>();
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) throw new KeyNotFoundException("Produto não encontrado.");
        return MapToDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var product = new Product(
            dto.Name,
            dto.Description,
            dto.Price,
            dto.ImageUrl,
            dto.Weight,
            dto.Width,
            dto.Height,
            dto.Length,
            dto.StockQuantity
        );

        var created = await _repository.CreateAsync(product);
        return MapToDto(created);
    }

    public async Task UpdateAsync(Guid id, CreateProductDto dto)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) throw new KeyNotFoundException("Produto não encontrado.");

        product.Update(
            dto.Name,
            dto.Description,
            dto.Price,
            dto.ImageUrl,
            dto.Weight,
            dto.Width,
            dto.Height,
            dto.Length,
            dto.StockQuantity
        );

        await _repository.UpdateAsync(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) throw new KeyNotFoundException("Produto não encontrado.");

        product.Deactivate();
        await _repository.UpdateAsync(product);
    }

    private static ProductResponseDto MapToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.Weight, p.Width, p.Height, p.Length, p.StockQuantity
    );
}