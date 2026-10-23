using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<List<Product>> GetActiveAsync(CancellationToken ct = default);

    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);
}