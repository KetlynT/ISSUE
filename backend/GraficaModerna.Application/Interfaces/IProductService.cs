using GraficaModerna.Application.DTOs;

namespace GraficaModerna.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductResponseDto>> GetCatalogAsync();
    Task<ProductResponseDto> GetByIdAsync(Guid id);
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(Guid id, CreateProductDto dto);
    Task DeleteAsync(Guid id);
}