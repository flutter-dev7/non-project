using Non.Application.Common;
using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Order?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default);

    Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
        OrderFilter filter, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(Order order, CancellationToken ct = default);
}