using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Enums;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GraficaModerna.Tests.Services;

public class OrderServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IShippingService> _shippingServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _emailServiceMock = new Mock<IEmailService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _shippingServiceMock = new Mock<IShippingService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<OrderService>>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        // Correção CS8625: Usando null! para satisfazer o compilador em argumentos de dependência não utilizados no mock
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _service = new OrderService(
            _context,
            _emailServiceMock.Object,
            _userManagerMock.Object,
            _httpContextAccessorMock.Object,
            [_shippingServiceMock.Object], // Correção IDE0028: Simplificação de coleção
            _paymentServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ShouldCreateOrder()
    {
        var userId = "user123";
        var product = new Product("P1", "D1", 100m, "img", 1, 10, 10, 10, 50);
        _context.Products.Add(product);

        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { Product = product, ProductId = product.Id, Quantity = 2 });
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        var addressDto = new CreateAddressDto("Home", "Me", "12345678", "St", "1", "", "Neigh", "City", "ST", "", "11999999999", false);
        var shippingOption = new ShippingOptionDto { Name = "Sedex", Price = 20m };

        _shippingServiceMock.Setup(s => s.CalculateAsync(It.IsAny<string>(), It.IsAny<List<ShippingItemDto>>()))
            .ReturnsAsync([shippingOption]); // Correção IDE0028

        var result = await _service.CreateOrderFromCartAsync(userId, addressDto, null, "Sedex");

        Assert.NotNull(result);
        Assert.Equal(220m, result.TotalAmount);
        Assert.Equal("Pendente", result.Status);

        var orderInDb = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == result.Id);
        Assert.NotNull(orderInDb);
        Assert.Single(orderInDb.Items);
        Assert.Empty(_context.CartItems);
    }

    [Fact]
    public async Task ConfirmPaymentViaWebhookAsync_ShouldUpdateStatusAndStock()
    {
        var product = new Product("P1", "D1", 100m, "img", 1, 10, 10, 10, 50);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = "u1",
            TotalAmount = 100m,
            Status = OrderStatus.Pendente,
            Items = [new() { ProductId = product.Id, Quantity = 10, UnitPrice = 10m }] // Correção IDE0028
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _service.ConfirmPaymentViaWebhookAsync(order.Id, "txn_123", 10000);

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        var updatedProduct = await _context.Products.FindAsync(product.Id);

        // Correção CS8602: Verificar nulidade antes de acessar propriedades
        Assert.NotNull(updatedOrder);
        Assert.NotNull(updatedProduct);

        Assert.Equal(OrderStatus.Pago, updatedOrder.Status);
        Assert.Equal("txn_123", updatedOrder.StripePaymentIntentId);
        Assert.Equal(40, updatedProduct.StockQuantity);
    }

    [Fact]
    public async Task RequestRefundAsync_ShouldSetRefundRequested()
    {
        var userId = "u1";
        var order = new Order { UserId = userId, Status = OrderStatus.Entregue, TotalAmount = 100m };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new RequestRefundDto("Total", null);

        await _service.RequestRefundAsync(order.Id, userId, dto);

        var updated = await _context.Orders.FindAsync(order.Id);

        // Correção CS8602: Verificar nulidade antes de acessar propriedades
        Assert.NotNull(updated);

        Assert.Equal("Total", updated.RefundType);
        Assert.Equal(100m, updated.RefundRequestedAmount);
    }

    [Fact]
    public async Task ConfirmPaymentViaWebhookAsync_ShouldDetectFraud_WhenAmountMismatch()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = "user1",
            TotalAmount = 200.00m, // Esperado: 20000 centavos
            Status = OrderStatus.Pendente
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Tentativa de pagar apenas 1 centavo num pedido de 200 reais
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.ConfirmPaymentViaWebhookAsync(order.Id, "txn_fraud", 1)); // 1 centavo

        Assert.Contains("Divergência de valores", ex.Message);

        // Verifica se o alerta de segurança foi enviado (email para admin)
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("ALERTA DE SEGURANÇA")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ShouldThrow_WhenCouponAlreadyUsed()
    {
        // Arrange
        var userId = "user_coupon_abuser";
        var product = new Product("P1", "D", 100m, "img", 1, 1, 1, 1, 10);
        _context.Products.Add(product);

        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { Product = product, ProductId = product.Id, Quantity = 1 });
        _context.Carts.Add(cart);

        var coupon = new Coupon("UNIQUETIME", 50, 10);
        _context.Coupons.Add(coupon);

        // Simula que o usuário JÁ usou este cupom antes
        _context.CouponUsages.Add(new CouponUsage
        {
            UserId = userId,
            CouponCode = "UNIQUETIME",
            OrderId = Guid.NewGuid()
        });

        await _context.SaveChangesAsync();

        var address = new CreateAddressDto("Casa", "Eu", "12345678", "Rua", "1", "", "Bairro", "Cidade", "UF", "", "1199999999", false);

        _shippingServiceMock.Setup(s => s.CalculateAsync(It.IsAny<string>(), It.IsAny<List<ShippingItemDto>>()))
            .ReturnsAsync([new() { Name = "Correios", Price = 10 }]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateOrderFromCartAsync(userId, address, "UNIQUETIME", "Correios"));

        Assert.Equal("Cupom já utilizado.", ex.Message);
    }

    [Fact]
    public async Task ConfirmPaymentViaWebhookAsync_DeveDetectarFraude_QuandoValorPagoForMenorQuePedido()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = "user_fraude",
            TotalAmount = 200.00m,
            Status = OrderStatus.Pendente
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.ConfirmPaymentViaWebhookAsync(order.Id, "txn_fraude_123", 1));

        Assert.Contains("Divergência de valores", ex.Message);

        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("ALERTA DE SEGURANÇA")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_DeveImpedirUso_DeCupomJaUtilizadoPeloUsuario()
    {
        // Arrange
        var userId = "user_cupom_duplicado";
        var product = new Product("P1", "D", 100m, "img", 1, 1, 1, 1, 10);
        _context.Products.Add(product);

        var cart = new Cart { UserId = userId };
        cart.Items.Add(new CartItem { Product = product, ProductId = product.Id, Quantity = 1 });
        _context.Carts.Add(cart);

        var coupon = new Coupon("PROMOUNIC", 50, 10);
        _context.Coupons.Add(coupon);

        _context.CouponUsages.Add(new CouponUsage
        {
            UserId = userId,
            CouponCode = "PROMOUNIC",
            OrderId = Guid.NewGuid()
        });

        await _context.SaveChangesAsync();

        var address = new CreateAddressDto("Casa", "Eu", "12345678", "Rua", "1", "", "Bairro", "Cidade", "UF", "", "1199999999", false);

        // CORREÇÃO IDE0028: Simplificação da lista de opções de frete
        _shippingServiceMock.Setup(s => s.CalculateAsync(It.IsAny<string>(), It.IsAny<List<ShippingItemDto>>()))
            .ReturnsAsync([new() { Name = "Sedex", Price = 10 }]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateOrderFromCartAsync(userId, address, "PROMOUNIC", "Sedex"));

        Assert.Equal("Cupom já utilizado.", ex.Message);
    }
}