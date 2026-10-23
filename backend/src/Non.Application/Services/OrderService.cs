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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository orderRepository,
        IShopRepository shopRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        ISettingRepository settingRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _shopRepository = shopRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
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

        order.RecordStatus(caller.Id);

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAsync(order.Id, ct);
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

        try
        {
            order.ChangeStatus(request.Status, caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAsync(order.Id, ct);
    }

    public async Task<Result<OrderDetailsResponse>> CancelAsync(
        Guid id, CurrentUser caller, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, ct);
        if (order is null)
            return Result<OrderDetailsResponse>.Failure("Заказ не найден.", ErrorType.NotFound);

        var allowed = caller.Role switch
        {
            UserRole.Admin => true,
            UserRole.Agent => order.CreatedByUserId == caller.Id,
            UserRole.Shop => await IsShopOwnerAsync(order.ShopId, caller.Id, ct),
            _ => false
        };

        if (!allowed)
            return Result<OrderDetailsResponse>.Failure("Недостаточно прав для отмены этого заказа.", ErrorType.Forbidden);

        try
        {
            order.Cancel(caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<OrderDetailsResponse>.Failure(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await ReloadAsync(order.Id, ct);
    }

    public async Task<Result<OrderDetailsResponse>> AssignCourierAsync(
        Guid id, AssignCourierRequest request, CancellationToken ct = default)
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

        return await ReloadAsync(order.Id, ct);
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

    private async Task<bool> IsShopOwnerAsync(Guid shopId, Guid userId, CancellationToken ct)
    {
        var shop = await _shopRepository.GetDetailsByOwnerIdAsync(userId, ct);

        return shop?.Id == shopId;
    }

    private async Task<Result<OrderDetailsResponse>> ReloadAsync(Guid id, CancellationToken ct)
    {
        var order = await _orderRepository.GetDetailsByIdAsync(id, ct);

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