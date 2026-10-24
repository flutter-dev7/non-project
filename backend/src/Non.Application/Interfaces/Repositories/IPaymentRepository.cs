using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> IsReversedAsync(Guid paymentId, CancellationToken ct = default);

    Task<HashSet<Guid>> GetReversedIdsAsync(IReadOnlyCollection<Guid> paymentIds, CancellationToken ct = default);

    Task<decimal> GetPaidTotalAsync(Guid shopId, CancellationToken ct = default);

    Task<Dictionary<Guid, decimal>> GetPaidTotalsByShopAsync(CancellationToken ct = default);

    Task<(List<Payment> Items, int TotalCount)> GetPagedAsync(
        Guid? shopId, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Payment payment, CancellationToken ct = default);
}