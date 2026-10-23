using Microsoft.EntityFrameworkCore;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

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

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }
}