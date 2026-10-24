using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IVisitRepository
{
    Task<Visit?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default);

    Task<(List<Visit> Items, int TotalCount)> GetPagedAsync(
        Guid? shopId, Guid? agentId, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Visit visit, CancellationToken ct = default);
}