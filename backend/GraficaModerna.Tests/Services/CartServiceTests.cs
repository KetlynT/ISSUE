using GraficaModerna.Application.DTOs;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GraficaModerna.Tests.Services;

public class CartServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _service;

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _uowMock = new Mock<IUnitOfWork>();
        _cartRepoMock = new Mock<ICartRepository>();
        _loggerMock = new Mock<ILogger<CartService>>();

        _uowMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);

        // Sincronização do Mock com o InMemory DB para lógica complexa
        _cartRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string uid) => _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.UserId == uid));

        _cartRepoMock.Setup(r => r.AddAsync(It.IsAny<Cart>()))
            .Callback((Cart c) => _context.Carts.Add(c));

        _service = new CartService(_uowMock.Object, _context, _loggerMock.Object);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAggregateQuantity_WhenItemAlreadyExists()
    {
        // Arrange: Adicionar o mesmo item deve somar a quantidade, não criar nova linha
        var userId = "user_agg";
        var product = new Product("P1", "D", 10m, "url", 1, 1, 1, 1, 100);
        _context.Products.Add(product);

        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { ProductId = product.Id, Quantity = 2 });
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var dto = new AddToCartDto(product.Id, 3);

        // Act
        await _service.AddItemAsync(userId, dto);

        // Assert
        var updatedCart = await _context.Carts.Include(c => c.Items).FirstAsync(c => c.UserId == userId);
        Assert.Single(updatedCart.Items); // Continua com 1 item na lista
        Assert.Equal(5, updatedCart.Items.First().Quantity); // 2 + 3 = 5
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrowException_WhenProductIsInactive()
    {
        // Arrange: Não permitir adicionar produtos inativos
        var userId = "user_inactive";
        var product = new Product("P_Inactive", "D", 10m, "url", 1, 1, 1, 1, 100);
        product.Deactivate(); //
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var dto = new AddToCartDto(product.Id, 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddItemAsync(userId, dto));

        Assert.Contains("indisponível", ex.Message);
    }

    [Fact]
    public async Task GetCartAsync_ShouldRemoveOrphanedItems()
    {
        // Arrange: Limpeza automática de itens que foram deletados/inativados
        var userId = "user_orphan";
        var activeProduct = new Product("Active", "D", 10m, "url", 1, 1, 1, 1, 10);
        var inactiveProduct = new Product("Inactive", "D", 10m, "url", 1, 1, 1, 1, 10);
        inactiveProduct.Deactivate();

        _context.Products.AddRange(activeProduct, inactiveProduct);

        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { Product = activeProduct, ProductId = activeProduct.Id, Quantity = 1 });
        cart.Items.Add(new CartItem { Product = inactiveProduct, ProductId = inactiveProduct.Id, Quantity = 1 });
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        // Configurar Mock para permitir a remoção (necessário pois o serviço chama o repo para remover)
        _cartRepoMock.Setup(r => r.RemoveItemAsync(It.IsAny<CartItem>()))
            .Callback((CartItem item) => {
                var dbItem = _context.CartItems.Local.FirstOrDefault(i => i.Id == item.Id);
                if (dbItem != null) _context.CartItems.Remove(dbItem);
            });

        // Act
        var result = await _service.GetCartAsync(userId);

        // Assert
        Assert.Single(result.Items); // Deve restar apenas 1 item
        Assert.Equal("Active", result.Items.First().ProductName);
    }
}