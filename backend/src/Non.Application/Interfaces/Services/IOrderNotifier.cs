using Non.Application.Common;
using Non.Domain.Entities;

namespace Non.Application.Interfaces.Services;

public interface IOrderNotifier
{
    Task NotifyAsync(OrderEvent kind, Order order, CurrentUser actor, CancellationToken ct = default);
}