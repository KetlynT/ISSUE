using GraficaModerna.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraficaModerna.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context) { _context = context; }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalOrders = await _context.Orders.CountAsync();

        // Receita líquida (exclui cancelados e reembolsados)
        var totalRevenue = await _context.Orders
            .Where(o => o.Status != "Cancelado" && o.Status != "Reembolsado")
            .SumAsync(o => o.TotalAmount);

        // Total estornado/reembolsado
        var totalRefunded = await _context.Orders
            .Where(o => o.Status == "Reembolsado")
            .SumAsync(o => o.TotalAmount);

        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pendente");

        var lowStockProducts = await _context.Products
            .Where(p => p.IsActive && p.StockQuantity < 10)
            .Select(p => new { p.Id, p.Name, p.StockQuantity })
            .OrderBy(p => p.StockQuantity).Take(5).ToListAsync();

        var recentOrders = await _context.Orders
            .OrderByDescending(o => o.OrderDate).Take(5)
            .Select(o => new { o.Id, o.TotalAmount, o.Status, Date = o.OrderDate }).ToListAsync();

        return Ok(new { totalOrders, totalRevenue, totalRefunded, pendingOrders, lowStockProducts, recentOrders });
    }
}