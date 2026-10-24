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

    Task ReloadAsync(Order order, CancellationToken ct = default);

    Task<decimal> GetDeliveredTotalAsync(Guid shopId, CancellationToken ct = default);

    Task<Dictionary<Guid, decimal>> GetDeliveredTotalsByShopAsync(CancellationToken ct = default);

    Task<decimal> GetPendingDebtTotalAsync(Guid shopId, CancellationToken ct = default);

    Task<List<DeliveredOrderInfo>> GetDeliveredOrdersAsync(Guid shopId, CancellationToken ct = default);

    Task<bool> CourierServesShopAsync(Guid courierId, Guid shopId, CancellationToken ct = default);

    Task<bool> ExistsForShopAsync(Guid orderId, Guid shopId, CancellationToken ct = default);
}