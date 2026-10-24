using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly AppDbContext _context;

    public VisitRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Visit?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Visits
            .AsNoTracking()
            .Include(v => v.Shop)
            .Include(v => v.Agent)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<(List<Visit> Items, int TotalCount)> GetPagedAsync(
        Guid? shopId, Guid? agentId, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Visits.AsNoTracking().AsQueryable();

        if (shopId.HasValue)
            query = query.Where(v => v.ShopId == shopId.Value);

        if (agentId.HasValue)
            query = query.Where(v => v.AgentId == agentId.Value);

        if (fromUtc.HasValue)
            query = query.Where(v => v.VisitedAt >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(v => v.VisitedAt < toUtc.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(v => v.Shop)
            .Include(v => v.Agent)
            .OrderByDescending(v => v.VisitedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Visit visit, CancellationToken ct = default)
    {
        await _context.Visits.AddAsync(visit, ct);
    }
}