using System.Security.Claims;
using GraficaModerna.Application.DTOs;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Domain.Constants;
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

public class OrderServiceAdminTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IShippingService> _shippingServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceAdminTests()
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
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _service = new OrderService(
            _context,
            _emailServiceMock.Object,
            _userManagerMock.Object,
            _httpContextAccessorMock.Object,
            new List<IShippingService> { _shippingServiceMock.Object },
            _paymentServiceMock.Object,
            _loggerMock.Object
        );
    }

    private void SetupHttpContext(string userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal)).Returns(userId);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldThrowUnauthorized_WhenUserIsNotAdmin()
    {
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Pago };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("user123", Roles.User);

        var dto = new UpdateOrderStatusDto("Enviado", null, null, null, null, null, null);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateAdminOrderAsync(orderId, dto));

        Assert.Equal("Apenas administradores podem alterar pedidos.", exception.Message);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldUpdateStatusAndTracking_AndLogHistory()
    {
        var orderId = Guid.NewGuid();
        var userId = "customer1";
        var user = new ApplicationUser { Id = userId, Email = "customer@test.com", FullName = "Customer" };
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.Pago,
            User = user
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        var dto = new UpdateOrderStatusDto("Enviado", "BR123456789", null, null, null, null, null);

        await _service.UpdateAdminOrderAsync(orderId, dto);

        var updatedOrder = await _context.Orders.Include(o => o.History).FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Enviado, updatedOrder.Status);
        Assert.Equal("BR123456789", updatedOrder.TrackingCode);

        Assert.Single(updatedOrder.History);
        var history = updatedOrder.History.First();
        Assert.Equal("Enviado", history.Status);
        Assert.Contains("Rastreio: BR123456789", history.Message);
        Assert.Equal("Admin:admin1", history.ChangedBy);

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            user.Email,
            It.Is<string>(s => s.Contains("Enviado")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldProcessRefund_WhenStatusIsReembolsado()
    {
        var orderId = Guid.NewGuid();
        var paymentIntentId = "pi_123456789";
        var totalAmount = 100.00m;

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Pago,
            TotalAmount = totalAmount,
            StripePaymentIntentId = paymentIntentId,
            RefundRequestedAmount = totalAmount
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);

        var dto = new UpdateOrderStatusDto("Reembolsado", null, null, null, null, null, null);

        await _service.UpdateAdminOrderAsync(orderId, dto);

        _paymentServiceMock.Verify(p => p.RefundPaymentAsync(paymentIntentId, totalAmount), Times.Once);

        var updatedOrder = await _context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.Reembolsado, updatedOrder!.Status);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldProcessPartialRefund_WhenAmountIsLessThanTotal()
    {
        var orderId = Guid.NewGuid();
        var paymentIntentId = "pi_partial";
        var totalAmount = 200.00m;
        var refundAmount = 50.00m;

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Pago,
            TotalAmount = totalAmount,
            StripePaymentIntentId = paymentIntentId
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);

        var dto = new UpdateOrderStatusDto("Reembolsado", null, null, null, null, null, refundAmount);

        await _service.UpdateAdminOrderAsync(orderId, dto);

        _paymentServiceMock.Verify(p => p.RefundPaymentAsync(paymentIntentId, refundAmount), Times.Once);

        var updatedOrder = await _context.Orders.Include(o => o.History).FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.Equal(OrderStatus.ReembolsadoParcialmente, updatedOrder!.Status);
        Assert.Contains(updatedOrder.History, h => h.Status == "Reembolsado Parcialmente");
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldThrowException_WhenRefundAmountExceedsTotal()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Pago,
            TotalAmount = 100.00m,
            StripePaymentIntentId = "pi_exceed"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);

        var dto = new UpdateOrderStatusDto("Reembolsado", null, null, null, null, null, 150.00m);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.UpdateAdminOrderAsync(orderId, dto));

        Assert.Contains("não pode ser maior que o total do pedido", exception.Message);
        _paymentServiceMock.Verify(p => p.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldUpdateReverseLogistics_WhenStatusIsAguardandoDevolucao()
    {
        var orderId = Guid.NewGuid();
        var userId = "user_dev";
        var user = new ApplicationUser { Id = userId, Email = "u@test.com" };
        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            Status = OrderStatus.ReembolsoSolicitado,
            User = user
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        var dto = new UpdateOrderStatusDto("Aguardando Devolução", null, "REV-CODE-123", "Instruções aqui", null, null, null);

        await _service.UpdateAdminOrderAsync(orderId, dto);

        var updatedOrder = await _context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.AguardandoDevolucao, updatedOrder!.Status);
        Assert.Equal("REV-CODE-123", updatedOrder.ReverseLogisticsCode);
        Assert.Equal("Instruções aqui", updatedOrder.ReturnInstructions);

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            user.Email,
            It.Is<string>(s => s.Contains("Instruções de Devolução")),
            It.Is<string>(b => b.Contains("REV-CODE-123"))), Times.Once);
    }

    [Fact]
    public async Task UpdateAdminOrderAsync_ShouldRollbackTransaction_WhenPaymentServiceFails()
    {
        var orderId = Guid.NewGuid();
        var paymentIntentId = "pi_fail";

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Pago,
            TotalAmount = 100.00m,
            StripePaymentIntentId = paymentIntentId
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        SetupHttpContext("admin1", Roles.Admin);

        _paymentServiceMock.Setup(p => p.RefundPaymentAsync(paymentIntentId, It.IsAny<decimal?>()))
            .ThrowsAsync(new Exception("Stripe Error"));

        var dto = new UpdateOrderStatusDto("Reembolsado", null, null, null, null, null, null);

        await Assert.ThrowsAsync<Exception>(() => _service.UpdateAdminOrderAsync(orderId, dto));

        var orderInDb = await _context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.Pago, orderInDb!.Status);
        Assert.Empty(_context.OrderHistories);
    }
}