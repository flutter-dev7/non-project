using Microsoft.EntityFrameworkCore;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        string? search, UserRole? role, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName + " " + u.LastName, pattern) ||
                EF.Functions.ILike(u.Phone, pattern));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
    
    public Task<List<User>> GetTrackedByFcmTokenAsync(string token, CancellationToken ct = default) =>
        _context.Users.Where(u => u.FcmToken == token).ToListAsync(ct);

    public Task<List<FcmTarget>> GetFcmTargetsByRoleAsync(UserRole role, CancellationToken ct = default) =>
        _context.Users
            .AsNoTracking()
            .Where(u => u.Role == role && u.IsActive && u.FcmToken != null && u.FcmToken != "")
            .Select(u => new FcmTarget(u.Id, u.FcmToken!))
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }
}