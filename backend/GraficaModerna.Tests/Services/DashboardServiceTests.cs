using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Enums;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GraficaModerna.Tests.Services;

public class DashboardServiceTests
{
    private readonly AppDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new DashboardService(_context);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateRevenueCorrectly()
    {
        // Arrange
        _context.Orders.AddRange(
            new Order { TotalAmount = 100m, Status = OrderStatus.Pago },
            new Order { TotalAmount = 200m, Status = OrderStatus.Entregue },
            new Order { TotalAmount = 50m, Status = OrderStatus.Cancelado }, // Não deve somar
            new Order { TotalAmount = 300m, Status = OrderStatus.Reembolsado } // Deve contar como reembolsado
        );
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        Assert.Equal(300m, stats.TotalRevenue); // 100 + 200 = 300
        Assert.Equal(300m, stats.TotalRefunded); // 300
        Assert.Equal(4, stats.TotalOrders); // Total de encomendas
    }

    [Fact]
    public async Task GetStatsAsync_ShouldIdentifyLowStockProducts()
    {
        // Arrange
        _context.Products.AddRange(
            new Product("P1", "D", 10, "url", 1, 1, 1, 1, 100), // OK
            new Product("P2", "D", 10, "url", 1, 1, 1, 1, 5),   // Baixo (<10)
            new Product("P3", "D", 10, "url", 1, 1, 1, 1, 0)    // Baixo (<10)
        );
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        Assert.Equal(2, stats.LowStockProducts.Count);
        Assert.Contains(stats.LowStockProducts, p => p.Name == "P2");
        Assert.Contains(stats.LowStockProducts, p => p.Name == "P3");
    }
}