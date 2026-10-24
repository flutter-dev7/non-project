using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IShopRepository
{
    Task<Shop?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Shop?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default);

    Task<Shop?> GetDetailsByOwnerIdAsync(Guid userId, CancellationToken ct = default);

    Task<List<Shop>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<bool> OwnerHasShopAsync(Guid userId, Guid? excludeShopId, CancellationToken ct = default);

    Task<(List<Shop> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Shop shop, CancellationToken ct = default);

    Task LockForUpdateAsync(Guid shopId, CancellationToken ct = default);
}