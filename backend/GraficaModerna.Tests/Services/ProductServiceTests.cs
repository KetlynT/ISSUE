using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Services;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace GraficaModerna.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly IMemoryCache _memoryCache;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new ProductService(
            _repositoryMock.Object,
            _memoryCache
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenExists()
    {
        var productId = Guid.NewGuid();
        var product = new Product(
            "Test",
            "Desc",
            10,
            "url",
            1,
            10,
            10,
            10,
            5
        );

        typeof(Product)
            .GetProperty("Id")?
            .SetValue(product, productId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var result = await _service.GetByIdAsync(productId);

        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetByIdAsync(productId)
        );
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedProduct()
    {
        var dto = new CreateProductDto(
            "New Product",
            "Desc",
            100,
            "http://img.com",
            2,
            20,
            20,
            20,
            10
        );

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) =>
            {
                typeof(Product)
                    .GetProperty("Id")?
                    .SetValue(p, Guid.NewGuid());

                return p;
            });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);

        _repositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<Product>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct_WhenExists()
    {
        var productId = Guid.NewGuid();
        var existingProduct = new Product(
            "Old",
            "Desc",
            10,
            "url",
            1,
            10,
            10,
            10,
            5
        );

        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        var dto = new UpdateProductDto(
            "Updated",
            "New Desc",
            20,
            "new_url",
            2,
            15,
            15,
            15,
            50
        );

        await _service.UpdateAsync(productId, dto);

        Assert.Equal("Updated", existingProduct.Name);
        Assert.Equal(20, existingProduct.Price);

        _repositoryMock.Verify(
            r => r.UpdateAsync(existingProduct),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeactivateProduct_WhenExists()
    {
        var productId = Guid.NewGuid();
        var product = new Product(
            "Test",
            "Desc",
            10,
            "url",
            1,
            10,
            10,
            10,
            5
        );

        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        await _service.DeleteAsync(productId);

        Assert.False(product.IsActive);

        _repositoryMock.Verify(
            r => r.UpdateAsync(product),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCatalogAsync_ShouldCacheResults()
    {
        // Arrange
        var products = new List<Product> { new("P1", "D", 10, "url", 1, 1, 1, 1, 10) };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((products, 1));

        // Act
        // Primeira chamada (deve ir ao repo)
        await _service.GetCatalogAsync(null, null, null, 1, 10);

        // Segunda chamada com mesmos parâmetros (deve pegar do cache - MemoryCache real foi instanciado no construtor)
        await _service.GetCatalogAsync(null, null, null, 1, 10);

        // Assert
        // Verifica se o repositório foi chamado APENAS UMA VEZ
        _repositoryMock.Verify(r => r.GetAllAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCatalogAsync_DeveUsarCache_NaSegundaChamada()
    {
        // Arrange
        var products = new List<Product> { new("Prod", "Desc", 10, "url", 1, 1, 1, 1, 10) };

        // Configura o mock para retornar os produtos
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((products, 1));

        // Act
        // 1ª Chamada: Deve ir ao repositório
        await _service.GetCatalogAsync(null, null, null, 1, 10);

        // 2ª Chamada: Deve pegar do Cache (mesmos parâmetros)
        await _service.GetCatalogAsync(null, null, null, 1, 10);

        // Assert
        // Verifica se o método GetAllAsync do repositório foi chamado EXATAMENTE UMA VEZ
        _repositoryMock.Verify(r => r.GetAllAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }
}
