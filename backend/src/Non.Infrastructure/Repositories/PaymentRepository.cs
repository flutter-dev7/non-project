using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Payments
            .AsNoTracking()
            .Include(p => p.Shop)
            .Include(p => p.ReceivedByUser)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> IsReversedAsync(Guid paymentId, CancellationToken ct = default) =>
        _context.Payments.AnyAsync(p => p.ReversalOfPaymentId == paymentId, ct);

    public async Task<HashSet<Guid>> GetReversedIdsAsync(
        IReadOnlyCollection<Guid> paymentIds, CancellationToken ct = default)
    {
        var ids = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ReversalOfPaymentId != null && paymentIds.Contains(p.ReversalOfPaymentId.Value))
            .Select(p => p.ReversalOfPaymentId!.Value)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task<decimal> GetPaidTotalAsync(Guid shopId, CancellationToken ct = default)
    {
        var total = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ShopId == shopId)
            .SumAsync(p => (decimal?)p.Amount, ct);

        return total ?? 0m;
    }

    public Task<Dictionary<Guid, decimal>> GetPaidTotalsByShopAsync(CancellationToken ct = default) =>
        _context.Payments
            .AsNoTracking()
            .GroupBy(p => p.ShopId)
            .Select(g => new { ShopId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.ShopId, x => x.Total, ct);

    public async Task<(List<Payment> Items, int TotalCount)> GetPagedAsync(
        Guid? shopId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Payments.AsNoTracking().AsQueryable();

        if (shopId.HasValue)
            query = query.Where(p => p.ShopId == shopId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(p => p.Shop)
            .Include(p => p.ReceivedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        await _context.Payments.AddAsync(payment, ct);
    }
}