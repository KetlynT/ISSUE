using AutoMapper;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;

namespace GraficaModerna.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetCatalogAsync()
    {
        var products = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        // O AutoMapper vai usar o construtor da entidade Product
        var product = _mapper.Map<Product>(dto);

        var created = await _repository.CreateAsync(product);
        return _mapper.Map<ProductResponseDto>(created);
    }

    public async Task UpdateAsync(Guid id, CreateProductDto dto)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) throw new KeyNotFoundException("Produto não encontrado.");

        // Atualiza usando o método de domínio
        product.Update(dto.Name, dto.Description, dto.Price, dto.ImageUrl);

        await _repository.UpdateAsync(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) throw new KeyNotFoundException("Produto não encontrado.");

        product.Deactivate(); // Soft Delete (desativação lógica)
        await _repository.UpdateAsync(product);
    }
}