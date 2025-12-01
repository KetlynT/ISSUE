using AutoMapper;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GraficaModerna.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public ProductService(IProductRepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResultDto<ProductResponseDto>> GetCatalogAsync(string? search, string? sort, string? order, int page, int pageSize)
    {
        // Chave única para o cache
        var cacheKey = $"catalog_{search}_{sort}_{order}_{page}_{pageSize}";

        // Tenta pegar do cache
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2); // Reduzi tempo pois estoque muda rápido

            var (products, totalCount) = await _repository.GetAllAsync(search, sort, order, page, pageSize);

            return new PagedResultDto<ProductResponseDto>
            {
                Items = _mapper.Map<IEnumerable<ProductResponseDto>>(products),
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }) ?? new PagedResultDto<ProductResponseDto>();
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var product = _mapper.Map<Product>(dto); // O AutoMapper usa o construtor atualizado automaticamente
        var created = await _repository.CreateAsync(product);
        return _mapper.Map<ProductResponseDto>(created);
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
            dto.StockQuantity // NOVO
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
}