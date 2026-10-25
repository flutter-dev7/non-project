using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, DateTime now, CancellationToken ct = default);

    Task<bool> TryRevokeAsync(Guid tokenId, Guid replacedByTokenId, DateTime now, CancellationToken ct = default);

    Task RemoveExpiredAsync(Guid userId, DateTime now, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);
}