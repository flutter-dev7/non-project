using System.Globalization;
using Non.Application.Common;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Common;
using Non.Domain.Constants;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class OrderService : IOrderService
{
    private static readonly TimeSpan CutoffCacheDuration = TimeSpan.FromMinutes(10);

    private readonly IOrderRepository _orderRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISettingRepository _settingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDebtService _debtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IOrderNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orderRepository,
        IShopRepository shopRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        ISettingRepository settingRepository,
        IPaymentRepository paymentRepository,
        IDebtService debtService,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IOrderNotifier notifier,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _shopRepository = shopRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _settingRepository = settingRepository;
        _paymentRepository = paymentRepository;
        _debtService = debtService;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<OrderResponse>>> GetPagedAsync(
        OrderQueryRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var filter = new OrderFilter
        {
            ShopId = request.ShopId,
            CourierId = request.CourierId,
            Status = request.Status,
            DeliveryDateFrom = request.DeliveryDateFrom,
            DeliveryDateTo = request.DeliveryDateTo
        };

        if (caller.Role == UserRole.Shop)
        {
            var shop = await _shopRepository.GetDetailsByOwnerIdAsync(caller.Id, ct);
            if (shop is null)
            {
                return Result<PagedResult<OrderResponse>>.Success(new PagedResult<OrderResponse>
                {
                    Page = page,
                    PageSize = pageSize
                });
            }

            filter.ShopId = shop.Id;
        }
        else if (caller.Role == UserRole.Courier)
        {
            filter.CourierId = caller.Id;
        }

        var (items, totalCount) = await _orderRepository.GetPagedAsync(filter, page, pageSize, ct);

        return Result<PagedResult<OrderResponse>>.Success(new PagedResult<OrderResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<OrderDetailsResponse>> GetByIdAsync(
        Guid id, CurrentUser caller, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetDetailsByIdAsync(id, ct);
        if (order is null || !CanView(order, caller))
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        return Result<OrderDetailsResponse>.Success(ToDetails(order));
    }

    public async Task<Result<OrderDetailsResponse>> CreateAsync(
        CreateOrderRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        var shopResult = await ResolveShopAsync(request, caller, ct);
        if (shopResult.IsFailure)
            return Result<OrderDetailsResponse>.Failure(shopResult.Error!, shopResult.ErrorType!.Value);

        var shop = shopResult.Value!;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        await _shopRepository.LockForUpdateAsync(shop.Id, ct);

        if (request.Id.HasValue)
        {
            var existing = await _orderRepository.GetDetailsByIdAsync(request.Id.Value, ct);
            if (existing is not null)
            {
                return existing.ShopId == shop.Id
                    ? Result<OrderDetailsResponse>.Success(ToDetails(existing))
                    : Result<OrderDetailsResponse>.Failure("Заказ с таким идентификатором уже существует.", ErrorType.Conflict);
            }
        }

        var dateError = await ValidateDeliveryDateAsync(request.DeliveryDate, caller.Role, ct);
        if (dateError is not null)
            return Result<OrderDetailsResponse>.Failure(dateError);

        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var products = await _productRepository.GetByIdsAsync(requested.Keys.ToList(), ct);
        var activeProducts = products.Where(p => p.IsActive).ToDictionary(p => p.Id);

        if (requested.Keys.Any(productId => !activeProducts.ContainsKey(productId)))
            return Result<OrderDetailsResponse>.Failure("Некоторые продукты не найдены или отключены.");

        var order = new Order
        {
            Id = request.Id ?? Guid.NewGuid(),
            ShopId = shop.Id,
            CreatedByUserId = caller.Id,
            Source = caller.Role == UserRole.Shop ? OrderSource.Shop : OrderSource.Agent,
            DeliveryDate = request.DeliveryDate,
            PaymentType = request.PaymentType,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        foreach (var (productId, quantity) in requested)
            order.AddItem(productId, quantity, activeProducts[productId].Price);

        if (request.PaymentType == PaymentType.Debt &&
            caller.Role != UserRole.Admin &&
            shop.DebtLimit.HasValue)
        {
            var balance = await _debtService.GetBalanceAsync(shop.Id, ct);
            var exposure = balance.Debt + balance.PendingDebtOrdersTotal + order.TotalAmount;

            if (exposure > shop.DebtLimit.Value)
            {
                return Result<OrderDetailsResponse>.Failure(
                    $"Превышен лимит долга магазина: лимит {shop.DebtLimit.Value.ToMoney()}, " +
                    $"долг с учётом заказов в работе и этого заказа {exposure.ToMoney()}.");
            }
        }

        order.RecordStatus(caller.Id);

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.Created, caller, ct);
    }

    public async Task<Result<OrderDetailsResponse>> UpdateAsync(
        Guid id, UpdateOrderRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetWithItemsForUpdateAsync(id, ct);
        if (order is null)
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        if (!await CanManageAsync(order, caller, ct))
            return Result<OrderDetailsResponse>.Failure("Недостаточно прав для изменения этого заказа.", ErrorType.Forbidden);

        if (!order.CanBeEdited)
            return Result<OrderDetailsResponse>.Failure("Заказ нельзя изменить после начала выпечки.");

        var windowError = await ValidateChangeWindowAsync(order, caller.Role, ct);
        if (windowError is not null)
            return Result<OrderDetailsResponse>.Failure(windowError);

        var dateError = await ValidateDeliveryDateAsync(request.DeliveryDate, caller.Role, ct);
        if (dateError is not null)
            return Result<OrderDetailsResponse>.Failure(dateError);

        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var products = await _productRepository.GetByIdsAsync(requested.Keys.ToList(), ct);
        var activeProducts = products.Where(p => p.IsActive).ToDictionary(p => p.Id);

        if (requested.Keys.Any(productId => !activeProducts.ContainsKey(productId)))
            return Result<OrderDetailsResponse>.Failure("Некоторые продукты не найдены или отключены.");

        var existingPrices = order.Items.ToDictionary(i => i.ProductId, i => i.UnitPrice);

        var lines = requested
            .Select(r => (
                ProductId: r.Key,
                Quantity: r.Value,
                UnitPrice: existingPrices.TryGetValue(r.Key, out var price) ? price : activeProducts[r.Key].Price))
            .ToList();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        await _shopRepository.LockForUpdateAsync(order.ShopId, ct);
        await _orderRepository.ReloadAsync(order, ct);

        if (!order.CanBeEdited)
            return Result<OrderDetailsResponse>.Failure("Заказ нельзя изменить после начала выпечки.");

        if (request.PaymentType == PaymentType.Debt && caller.Role != UserRole.Admin)
        {
            var shop = await _shopRepository.GetDetailsByIdAsync(order.ShopId, ct);

            if (shop?.DebtLimit is { } limit)
            {
                var newTotal = lines.Sum(l => l.Quantity * l.UnitPrice);
                var balance = await _debtService.GetBalanceAsync(order.ShopId, ct);
                var ownPending = order.PaymentType == PaymentType.Debt ? order.TotalAmount : 0m;
                var exposure = balance.Debt + balance.PendingDebtOrdersTotal - ownPending + newTotal;

                if (exposure > limit)
                {
                    return Result<OrderDetailsResponse>.Failure(
                        $"Превышен лимит долга магазина: лимит {limit.ToMoney()}, " +
                        $"долг с учётом заказов в работе и этого заказа {exposure.ToMoney()}.");
                }
            }
        }

        order.DeliveryDate = request.DeliveryDate;
        order.PaymentType = request.PaymentType;
        order.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        try
        {
            order.ReplaceItems(lines);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.Updated, caller, ct);
    }

    public async Task<Result<OrderDetailsResponse>> ChangeStatusAsync(
        Guid id, ChangeOrderStatusRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        if (request.Status == OrderStatus.Cancelled)
            return Result<OrderDetailsResponse>.Failure("Для отмены заказа используйте отдельную операцию отмены.");

        var order = await _orderRepository.GetByIdAsync(id, ct);
        if (order is null)
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        if (caller.Role == UserRole.Courier)
        {
            if (order.CourierId != caller.Id)
                return Result<OrderDetailsResponse>.Failure("Этот заказ назначен не вам.", ErrorType.Forbidden);

            if (request.Status is not (OrderStatus.OnTheWay or OrderStatus.Delivered))
                return Result<OrderDetailsResponse>.Failure(
                    "Курьер может только забрать заказ (В пути) или отметить его доставленным.", ErrorType.Forbidden);
        }
        else if (caller.Role != UserRole.Admin)
        {
            return Result<OrderDetailsResponse>.Failure("Недостаточно прав.", ErrorType.Forbidden);
        }

        if (request.Status == OrderStatus.OnTheWay && order.CourierId is null)
            return Result<OrderDetailsResponse>.Failure("Сначала назначьте курьера.");

        if (request.Status != OrderStatus.Delivered && request.PaymentAmount.HasValue)
            return Result<OrderDetailsResponse>.Failure("Сумма оплаты указывается только при доставке.");

        if (request.Status == OrderStatus.Delivered)
            return await DeliverAsync(order, request, caller, ct);

        try
        {
            order.ChangeStatus(request.Status, caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.StatusChanged, caller, ct);
    }

    public async Task<Result<OrderDetailsResponse>> CancelAsync(
        Guid id, CurrentUser caller, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, ct);
        if (order is null)
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        if (!await CanManageAsync(order, caller, ct))
            return Result<OrderDetailsResponse>.Failure("Недостаточно прав для отмены этого заказа.", ErrorType.Forbidden);

        var windowError = await ValidateChangeWindowAsync(order, caller.Role, ct);
        if (windowError is not null)
            return Result<OrderDetailsResponse>.Failure(windowError);

        try
        {
            order.Cancel(caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.Cancelled, caller, ct);
    }

    public async Task<Result<OrderDetailsResponse>> AssignCourierAsync(
        Guid id, AssignCourierRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, ct);
        if (order is null)
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result<OrderDetailsResponse>.Failure("Нельзя назначить курьера для завершённого или отменённого заказа.");

        var courier = await _userRepository.GetByIdAsync(request.CourierId, ct);
        if (courier is null)
            return Result<OrderDetailsResponse>.Failure("Курьер не найден.", ErrorType.NotFound);

        if (courier.Role != UserRole.Courier || !courier.IsActive)
            return Result<OrderDetailsResponse>.Failure("Пользователь не является активным курьером.");

        order.CourierId = courier.Id;
        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.CourierAssigned, caller, ct);
    }

    private async Task<Result<OrderDetailsResponse>> DeliverAsync(
        Order order, ChangeOrderStatusRequest request, CurrentUser caller, CancellationToken ct)
    {
        if (order.PaymentType == PaymentType.Now && request.PaymentAmount is null)
            return Result<OrderDetailsResponse>.Failure("Для заказа с оплатой «Сразу» укажите принятую сумму.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        await _shopRepository.LockForUpdateAsync(order.ShopId, ct);
        await _orderRepository.ReloadAsync(order, ct);

        try
        {
            order.ChangeStatus(OrderStatus.Delivered, caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        if (request.PaymentAmount.HasValue)
        {
            var balance = await _debtService.GetBalanceAsync(order.ShopId, ct);
            var debtAfterDelivery = balance.Debt + order.TotalAmount;

            if (request.PaymentAmount.Value > debtAfterDelivery)
            {
                return Result<OrderDetailsResponse>.Failure(
                    $"Сумма оплаты ({request.PaymentAmount.Value.ToMoney()}) превышает долг магазина " +
                    $"({debtAfterDelivery.ToMoney()}).");
            }

            Payment payment;
            try
            {
                payment = Payment.Create(
                    order.ShopId,
                    order.Id,
                    request.PaymentAmount.Value,
                    request.PaymentMethod ?? PaymentMethod.Cash,
                    caller.Id);
            }
            catch (DomainException ex)
            {
                return Result<OrderDetailsResponse>.Failure(ex.Message);
            }

            await _paymentRepository.AddAsync(payment, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await ReloadAndNotifyAsync(order.Id, OrderEvent.StatusChanged, caller, ct);
    }

    private async Task<Result<Shop>> ResolveShopAsync(
        CreateOrderRequest request, CurrentUser caller, CancellationToken ct)
    {
        Shop? shop;

        if (caller.Role == UserRole.Shop)
        {
            shop = await _shopRepository.GetDetailsByOwnerIdAsync(caller.Id, ct);
            if (shop is null)
                return Result<Shop>.Failure("Сначала создайте свой магазин.");
        }
        else if (caller.Role is UserRole.Agent or UserRole.Admin)
        {
            if (request.ShopId is null)
                return Result<Shop>.Failure("Укажите магазин.");

            shop = await _shopRepository.GetDetailsByIdAsync(request.ShopId.Value, ct);
            if (shop is null)
                return Result<Shop>.Failure("Магазин не найден.", ErrorType.NotFound);
        }
        else
        {
            return Result<Shop>.Failure("Недостаточно прав.", ErrorType.Forbidden);
        }

        if (!shop.IsActive)
            return Result<Shop>.Failure("Магазин отключён.");

        return Result<Shop>.Success(shop);
    }

    private async Task<string?> ValidateDeliveryDateAsync(DateOnly deliveryDate, UserRole role, CancellationToken ct)
    {
        var nowLocal = _timeProvider.GetUtcNow().ToOffset(BusinessRules.UtcOffset).DateTime;
        var today = DateOnly.FromDateTime(nowLocal);

        if (deliveryDate <= today)
            return "Дата доставки должна быть не раньше завтрашнего дня.";

        if (deliveryDate > today.AddDays(BusinessRules.MaxDeliveryDaysAhead))
            return $"Заказ можно оформить не более чем на {BusinessRules.MaxDeliveryDaysAhead} дней вперёд.";

        if (role != UserRole.Admin && deliveryDate == today.AddDays(1))
        {
            var cutoff = await GetCutoffTimeAsync(ct);

            if (TimeOnly.FromDateTime(nowLocal) > cutoff)
                return $"Заказы на завтра принимаются до {cutoff.ToString("HH:mm", CultureInfo.InvariantCulture)}.";
        }

        return null;
    }

    private async Task<string?> ValidateChangeWindowAsync(Order order, UserRole role, CancellationToken ct)
    {
        if (role == UserRole.Admin)
            return null;

        var nowLocal = _timeProvider.GetUtcNow().ToOffset(BusinessRules.UtcOffset).DateTime;
        var today = DateOnly.FromDateTime(nowLocal);

        if (order.DeliveryDate <= today)
            return "Заказ на сегодня или прошедшую дату изменить нельзя.";

        if (order.DeliveryDate == today.AddDays(1))
        {
            var cutoff = await GetCutoffTimeAsync(ct);

            if (TimeOnly.FromDateTime(nowLocal) > cutoff)
                return $"Изменение и отмена заказов на завтра закрыты после {cutoff.ToString("HH:mm", CultureInfo.InvariantCulture)}.";
        }

        return null;
    }

    private async Task<TimeOnly> GetCutoffTimeAsync(CancellationToken ct)
    {
        var value = await _cacheService.GetOrCreateAsync(
            CacheKeys.OrderCutoffTime,
            async () => await _settingRepository.GetValueAsync(SettingKeys.OrderCutoffTime, ct)
                        ?? SettingDefaults.OrderCutoffTime,
            CutoffCacheDuration);

        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var cutoff)
            ? cutoff
            : TimeOnly.Parse(SettingDefaults.OrderCutoffTime, CultureInfo.InvariantCulture);
    }

    private async Task<bool> CanManageAsync(Order order, CurrentUser caller, CancellationToken ct) =>
        caller.Role switch
        {
            UserRole.Admin => true,
            UserRole.Agent => order.CreatedByUserId == caller.Id,
            UserRole.Shop => await IsShopOwnerAsync(order.ShopId, caller.Id, ct),
            _ => false
        };

    private async Task<bool> IsShopOwnerAsync(Guid shopId, Guid userId, CancellationToken ct)
    {
        var shop = await _shopRepository.GetDetailsByOwnerIdAsync(userId, ct);

        return shop?.Id == shopId;
    }

    private async Task<Result<OrderDetailsResponse>> ReloadAndNotifyAsync(
        Guid id, OrderEvent kind, CurrentUser actor, CancellationToken ct)
    {
        var order = await _orderRepository.GetDetailsByIdAsync(id, ct);

        await _notifier.NotifyAsync(kind, order!, actor, ct);

        return Result<OrderDetailsResponse>.Success(ToDetails(order!));
    }

    private static bool CanView(Order order, CurrentUser caller) => caller.Role switch
    {
        UserRole.Admin or UserRole.Agent => true,
        UserRole.Shop => order.Shop?.UserId == caller.Id,
        UserRole.Courier => order.CourierId == caller.Id,
        _ => false
    };

    private static OrderResponse ToResponse(Order order) => FillResponse(new OrderResponse(), order);

    private static OrderDetailsResponse ToDetails(Order order)
    {
        var details = FillResponse(new OrderDetailsResponse(), order);

        details.History = order.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusHistoryResponse
            {
                Status = h.Status.ToString(),
                ChangedByUserId = h.ChangedByUserId,
                ChangedByName = h.ChangedByUser?.FullName,
                ChangedAt = h.ChangedAt
            })
            .ToList();

        return details;
    }

    private static T FillResponse<T>(T response, Order order) where T : OrderResponse
    {
        response.Id = order.Id;
        response.ShopId = order.ShopId;
        response.ShopName = order.Shop?.Name ?? string.Empty;
        response.ShopAddress = order.Shop?.Address ?? string.Empty;
        response.ShopPhone = order.Shop?.Phone ?? string.Empty;
        response.Status = order.Status.ToString();
        response.DeliveryDate = order.DeliveryDate;
        response.PaymentType = order.PaymentType.ToString();
        response.TotalAmount = order.TotalAmount;
        response.CourierId = order.CourierId;
        response.CourierName = order.Courier?.FullName;
        response.Source = order.Source.ToString();
        response.Note = order.Note;
        response.CreatedAt = order.CreatedAt;
        response.Items = order.Items
            .Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            })
            .ToList();

        return response;
    }
}