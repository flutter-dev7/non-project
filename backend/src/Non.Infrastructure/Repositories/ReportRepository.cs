using Microsoft.EntityFrameworkCore;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> CountOrdersForProductionAsync(DateOnly date, CancellationToken ct = default) =>
        _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.DeliveryDate == date && o.Status != OrderStatus.Cancelled, ct);

    public Task<List<ProductionRow>> GetProductionRowsAsync(DateOnly date, CancellationToken ct = default) =>
        _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order!.DeliveryDate == date && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => new { i.ProductId, i.Product!.Name })
            .Select(g => new ProductionRow(g.Key.ProductId, g.Key.Name, g.Sum(i => i.Quantity)))
            .ToListAsync(ct);

    public Task<List<SalesDayRow>> GetSalesByDayAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveryDate >= from && o.DeliveryDate <= to)
            .GroupBy(o => o.DeliveryDate)
            .Select(g => new SalesDayRow(g.Key, g.Count(), g.Sum(o => o.TotalAmount)))
            .ToListAsync(ct);

    public Task<List<SalesProductRow>> GetSalesByProductAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order!.Status == OrderStatus.Delivered
                        && i.Order.DeliveryDate >= from
                        && i.Order.DeliveryDate <= to)
            .GroupBy(i => new { i.ProductId, i.Product!.Name })
            .Select(g => new SalesProductRow(
                g.Key.ProductId,
                g.Key.Name,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.Quantity * i.UnitPrice)))
            .ToListAsync(ct);

    public async Task<decimal> GetPaymentsTotalAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var total = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
            .SumAsync(p => (decimal?)p.Amount, ct);

        return total ?? 0m;
    }
}