using Non.Application.Common;
using Non.Application.DTOs.Debts.Request;
using Non.Application.DTOs.Debts.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class DebtService : IDebtService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IShopRepository _shopRepository;

    public DebtService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IShopRepository shopRepository)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _shopRepository = shopRepository;
    }

    public async Task<ShopBalance> GetBalanceAsync(Guid shopId, CancellationToken ct = default)
    {
        var deliveredTotal = await _orderRepository.GetDeliveredTotalAsync(shopId, ct);
        var paidTotal = await _paymentRepository.GetPaidTotalAsync(shopId, ct);
        var pendingDebtTotal = await _orderRepository.GetPendingDebtTotalAsync(shopId, ct);

        return new ShopBalance(deliveredTotal, paidTotal, pendingDebtTotal);
    }

    public async Task<Result<ShopBalanceResponse>> GetShopBalanceAsync(Guid shopId, CancellationToken ct = default)
    {
        var shop = await _shopRepository.GetDetailsByIdAsync(shopId, ct);
        if (shop is null)
            return Result<ShopBalanceResponse>.Failure("Магазин не найден.", ErrorType.NotFound);

        return Result<ShopBalanceResponse>.Success(await BuildBalanceAsync(shop, ct));
    }

    public async Task<Result<ShopBalanceResponse>> GetMyBalanceAsync(CurrentUser caller, CancellationToken ct = default)
    {
        var shop = await _shopRepository.GetDetailsByOwnerIdAsync(caller.Id, ct);
        if (shop is null)
            return Result<ShopBalanceResponse>.Failure("У вас ещё нет магазина.", ErrorType.NotFound);

        return Result<ShopBalanceResponse>.Success(await BuildBalanceAsync(shop, ct));
    }

    public async Task<Result<PagedResult<DebtListItemResponse>>> GetDebtsAsync(
        DebtQueryRequest request, CancellationToken ct = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var deliveredTotals = await _orderRepository.GetDeliveredTotalsByShopAsync(ct);
        var paidTotals = await _paymentRepository.GetPaidTotalsByShopAsync(ct);

        var debts = deliveredTotals
            .Select(x => new { ShopId = x.Key, Debt = x.Value - paidTotals.GetValueOrDefault(x.Key) })
            .Where(x => x.Debt > 0)
            .OrderByDescending(x => x.Debt)
            .ToList();

        var pageItems = debts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var shops = (await _shopRepository.GetByIdsAsync(pageItems.Select(x => x.ShopId).ToList(), ct))
            .ToDictionary(s => s.Id);

        var items = pageItems
            .Where(x => shops.ContainsKey(x.ShopId))
            .Select(x =>
            {
                var shop = shops[x.ShopId];

                return new DebtListItemResponse
                {
                    ShopId = shop.Id,
                    ShopName = shop.Name,
                    ShopPhone = shop.Phone,
                    Debt = x.Debt,
                    DebtLimit = shop.DebtLimit,
                    IsOverLimit = shop.DebtLimit.HasValue && x.Debt > shop.DebtLimit.Value
                };
            })
            .ToList();

        return Result<PagedResult<DebtListItemResponse>>.Success(new PagedResult<DebtListItemResponse>
        {
            Items = items,
            TotalCount = debts.Count,
            Page = page,
            PageSize = pageSize
        });
    }

    private async Task<ShopBalanceResponse> BuildBalanceAsync(Shop shop, CancellationToken ct)
    {
        var balance = await GetBalanceAsync(shop.Id, ct);
        var deliveredOrders = await _orderRepository.GetDeliveredOrdersAsync(shop.Id, ct);

        var unpaidOrders = DebtCalculator.Allocate(deliveredOrders, balance.PaidTotal)
            .Where(o => o.RemainingAmount > 0)
            .Select(o => new UnpaidOrderResponse
            {
                OrderId = o.OrderId,
                DeliveryDate = o.DeliveryDate,
                TotalAmount = o.TotalAmount,
                PaidAmount = o.PaidAmount,
                RemainingAmount = o.RemainingAmount
            })
            .ToList();

        return new ShopBalanceResponse
        {
            ShopId = shop.Id,
            ShopName = shop.Name,
            DeliveredTotal = balance.DeliveredTotal,
            PaidTotal = balance.PaidTotal,
            Debt = balance.Debt,
            PendingDebtOrdersTotal = balance.PendingDebtOrdersTotal,
            DebtLimit = shop.DebtLimit,
            UnpaidOrders = unpaidOrders
        };
    }
}