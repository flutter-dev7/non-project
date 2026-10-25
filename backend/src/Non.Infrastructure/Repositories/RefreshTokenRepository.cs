using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.Set<RefreshToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, DateTime now, CancellationToken ct = default) =>
        _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

    public async Task<bool> TryRevokeAsync(
        Guid tokenId, Guid replacedByTokenId, DateTime now, CancellationToken ct = default)
    {
        var affected = await _context.Set<RefreshToken>()
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, (DateTime?)now)
                .SetProperty(t => t.ReplacedByTokenId, (Guid?)replacedByTokenId), ct);

        return affected == 1;
    }

    public async Task RemoveExpiredAsync(Guid userId, DateTime now, CancellationToken ct = default)
    {
        var expired = await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.ExpiresAt <= now)
            .ToListAsync(ct);

        _context.Set<RefreshToken>().RemoveRange(expired);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _context.Set<RefreshToken>().AddAsync(token, ct);
    }
}