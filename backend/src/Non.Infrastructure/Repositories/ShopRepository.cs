using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class ShopRepository : IShopRepository
{
    private readonly AppDbContext _context;

    public ShopRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Shop?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Shops.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Shop?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Shops
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Shop?> GetDetailsByOwnerIdAsync(Guid userId, CancellationToken ct = default) =>
        _context.Shops
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public Task<bool> OwnerHasShopAsync(Guid userId, Guid? excludeShopId, CancellationToken ct = default) =>
        _context.Shops.AnyAsync(
            s => s.UserId == userId && (excludeShopId == null || s.Id != excludeShopId), ct);

    public async Task<(List<Shop> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Shops.AsNoTracking().Include(s => s.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, pattern) ||
                EF.Functions.ILike(s.Address, pattern) ||
                EF.Functions.ILike(s.Phone, pattern));
        }

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
    
    public Task<List<Shop>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        _context.Shops
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);

    public Task LockForUpdateAsync(Guid shopId, CancellationToken ct = default) =>
        _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT \"Id\" FROM \"Shops\" WHERE \"Id\" = {shopId} FOR UPDATE", ct);

    public async Task AddAsync(Shop shop, CancellationToken ct = default)
    {
        await _context.Shops.AddAsync(shop, ct);
    }
}