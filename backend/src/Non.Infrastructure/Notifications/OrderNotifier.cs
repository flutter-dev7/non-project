using System.Globalization;
using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Infrastructure.Notifications;

public class OrderNotifier : IOrderNotifier
{
    private readonly IShopRepository _shops;
    private readonly IUserRepository _users;
    private readonly IPushQueue _queue;
    private readonly ILogger<OrderNotifier> _logger;

    public OrderNotifier(
        IShopRepository shops,
        IUserRepository users,
        IPushQueue queue,
        ILogger<OrderNotifier> logger)
    {
        _shops = shops;
        _users = users;
        _queue = queue;
        _logger = logger;
    }

    public async Task NotifyAsync(OrderEvent kind, Order order, CurrentUser actor, CancellationToken ct = default)
    {
        try
        {
            await DispatchAsync(kind, order, actor, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подготовить уведомления для заказа {OrderId}.", order.Id);
        }
    }

    private async Task DispatchAsync(OrderEvent kind, Order order, CurrentUser actor, CancellationToken ct)
    {
        var shop = await _shops.GetDetailsByIdAsync(order.ShopId, ct);
        var shopName = shop?.Name ?? "Магазин";
        var ownerId = shop?.UserId;

        var date = order.DeliveryDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var details = $"на {date}, сумма {order.TotalAmount.ToMoney()}";

        switch (kind)
        {
            case OrderEvent.Created:
                if (actor.Role != UserRole.Admin)
                    await NotifyAdminsAsync(actor, order, "order_created", "Новый заказ", $"{shopName}: {details}", ct);

                await NotifyUserAsync(ownerId, actor, order, "order_created",
                    "Оформлен заказ для вашего магазина", details, ct);
                break;

            case OrderEvent.StatusChanged:
                await NotifyUserAsync(ownerId, actor, order, "order_status",
                    StatusTitle(order.Status), $"Заказ {details}", ct);
                break;

            case OrderEvent.CourierAssigned:
                await NotifyUserAsync(order.CourierId, actor, order, "order_assigned",
                    "Вам назначен заказ", $"{shopName}, {shop?.Address}: {details}", ct);
                break;

            case OrderEvent.Updated:
                if (actor.Role != UserRole.Admin)
                    await NotifyAdminsAsync(actor, order, "order_updated", "Заказ изменён", $"{shopName}: {details}", ct);

                await NotifyUserAsync(order.CourierId, actor, order, "order_updated",
                    "Заказ изменён", $"{shopName}: {details}", ct);
                break;

            case OrderEvent.Cancelled:
                if (actor.Role != UserRole.Admin)
                    await NotifyAdminsAsync(actor, order, "order_cancelled", "Заказ отменён", $"{shopName}: {details}", ct);

                await NotifyUserAsync(order.CourierId, actor, order, "order_cancelled",
                    "Заказ отменён", $"{shopName}: {details}", ct);

                await NotifyUserAsync(ownerId, actor, order, "order_cancelled",
                    "Заказ отменён", $"Заказ {details}", ct);
                break;
        }
    }

    private async Task NotifyAdminsAsync(
        CurrentUser actor, Order order, string type, string title, string body, CancellationToken ct)
    {
        var admins = await _users.GetFcmTargetsByRoleAsync(UserRole.Admin, ct);

        foreach (var admin in admins.Where(a => a.UserId != actor.Id))
            _queue.Enqueue(new PushMessage(admin.Token, title, body, BuildData(order, type)));
    }

    private async Task NotifyUserAsync(
        Guid? userId, CurrentUser actor, Order order, string type, string title, string body, CancellationToken ct)
    {
        if (userId is null || userId == actor.Id)
            return;

        var user = await _users.GetByIdAsync(userId.Value, ct);

        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.FcmToken))
            return;

        _queue.Enqueue(new PushMessage(user.FcmToken, title, body, BuildData(order, type)));
    }

    private static Dictionary<string, string> BuildData(Order order, string type) => new()
    {
        ["type"] = type,
        ["orderId"] = order.Id.ToString(),
        ["shopId"] = order.ShopId.ToString(),
        ["status"] = order.Status.ToString()
    };

    private static string StatusTitle(OrderStatus status) => status switch
    {
        OrderStatus.Accepted => "Заказ принят",
        OrderStatus.Baking => "Заказ готовится",
        OrderStatus.OnTheWay => "Заказ в пути",
        OrderStatus.Delivered => "Заказ доставлен",
        OrderStatus.Cancelled => "Заказ отменён",
        _ => "Статус заказа изменён"
    };
}