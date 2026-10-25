using Microsoft.EntityFrameworkCore;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    private static readonly OrderStatus[] PendingStatuses =
    {
        OrderStatus.New, OrderStatus.Accepted, OrderStatus.Baking, OrderStatus.OnTheWay
    };

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.Shop)
            .Include(o => o.Courier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
        OrderFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Orders.AsNoTracking().AsQueryable();

        if (filter.ShopId.HasValue)
            query = query.Where(o => o.ShopId == filter.ShopId.Value);

        if (filter.CourierId.HasValue)
            query = query.Where(o => o.CourierId == filter.CourierId.Value);

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        if (filter.DeliveryDateFrom.HasValue)
            query = query.Where(o => o.DeliveryDate >= filter.DeliveryDateFrom.Value);

        if (filter.DeliveryDateTo.HasValue)
            query = query.Where(o => o.DeliveryDate <= filter.DeliveryDateTo.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .AsSplitQuery()
            .Include(o => o.Shop)
            .Include(o => o.Courier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.DeliveryDate)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
    
    public Task<Order?> GetWithItemsForUpdateAsync(Guid id, CancellationToken ct = default) =>
        _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }

    public async Task ReloadAsync(Order order, CancellationToken ct = default)
    {
        await _context.Entry(order).ReloadAsync(ct);
    }

    public async Task<decimal> GetDeliveredTotalAsync(Guid shopId, CancellationToken ct = default)
    {
        var total = await _context.Orders
            .AsNoTracking()
            .Where(o => o.ShopId == shopId && o.Status == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.TotalAmount, ct);

        return total ?? 0m;
    }

    public Task<Dictionary<Guid, decimal>> GetDeliveredTotalsByShopAsync(CancellationToken ct = default) =>
        _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered)
            .GroupBy(o => o.ShopId)
            .Select(g => new { ShopId = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToDictionaryAsync(x => x.ShopId, x => x.Total, ct);

    public async Task<decimal> GetPendingDebtTotalAsync(Guid shopId, CancellationToken ct = default)
    {
        var total = await _context.Orders
            .AsNoTracking()
            .Where(o => o.ShopId == shopId
                        && o.PaymentType == PaymentType.Debt
                        && PendingStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount, ct);

        return total ?? 0m;
    }

    public Task<List<DeliveredOrderInfo>> GetDeliveredOrdersAsync(Guid shopId, CancellationToken ct = default) =>
        _context.Orders
            .AsNoTracking()
            .Where(o => o.ShopId == shopId && o.Status == OrderStatus.Delivered)
            .Select(o => new DeliveredOrderInfo(o.Id, o.DeliveryDate, o.CreatedAt, o.TotalAmount))
            .ToListAsync(ct);

    public Task<bool> CourierServesShopAsync(Guid courierId, Guid shopId, CancellationToken ct = default) =>
        _context.Orders.AnyAsync(
            o => o.CourierId == courierId && o.ShopId == shopId && o.Status != OrderStatus.Cancelled, ct);

    public Task<bool> ExistsForShopAsync(Guid orderId, Guid shopId, CancellationToken ct = default) =>
        _context.Orders.AnyAsync(o => o.Id == orderId && o.ShopId == shopId, ct);
}