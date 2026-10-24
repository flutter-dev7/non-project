using Non.Application.Common;
using Non.Application.DTOs.Payments.Request;
using Non.Application.DTOs.Payments.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Common;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDebtService _debtService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IShopRepository shopRepository,
        IOrderRepository orderRepository,
        IDebtService debtService,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _shopRepository = shopRepository;
        _orderRepository = orderRepository;
        _debtService = debtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<PaymentResponse>>> GetPagedAsync(
        PaymentQueryRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        Guid? shopId;

        if (caller.Role == UserRole.Shop)
        {
            var shop = await _shopRepository.GetDetailsByOwnerIdAsync(caller.Id, ct);
            if (shop is null)
            {
                return Result<PagedResult<PaymentResponse>>.Success(new PagedResult<PaymentResponse>
                {
                    Page = page,
                    PageSize = pageSize
                });
            }

            shopId = shop.Id;
        }
        else if (caller.Role == UserRole.Admin)
        {
            shopId = request.ShopId;
        }
        else
        {
            return Result<PagedResult<PaymentResponse>>.Failure("Недостаточно прав.", ErrorType.Forbidden);
        }

        var (items, totalCount) = await _paymentRepository.GetPagedAsync(shopId, page, pageSize, ct);
        var reversedIds = await _paymentRepository.GetReversedIdsAsync(items.Select(p => p.Id).ToList(), ct);

        return Result<PagedResult<PaymentResponse>>.Success(new PagedResult<PaymentResponse>
        {
            Items = items.Select(p => ToResponse(p, reversedIds.Contains(p.Id))).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        if (caller.Role is not (UserRole.Admin or UserRole.Courier))
            return Result<PaymentResponse>.Failure("Недостаточно прав.", ErrorType.Forbidden);

        var shop = await _shopRepository.GetDetailsByIdAsync(request.ShopId, ct);
        if (shop is null)
            return Result<PaymentResponse>.Failure("Магазин не найден.", ErrorType.NotFound);

        if (caller.Role == UserRole.Courier &&
            !await _orderRepository.CourierServesShopAsync(caller.Id, shop.Id, ct))
        {
            return Result<PaymentResponse>.Failure("Вы не обслуживаете этот магазин.", ErrorType.Forbidden);
        }

        if (request.OrderId.HasValue &&
            !await _orderRepository.ExistsForShopAsync(request.OrderId.Value, shop.Id, ct))
        {
            return Result<PaymentResponse>.Failure("Заказ не найден в этом магазине.", ErrorType.NotFound);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        await _shopRepository.LockForUpdateAsync(shop.Id, ct);

        var balance = await _debtService.GetBalanceAsync(shop.Id, ct);
        if (request.Amount > balance.Debt)
        {
            return Result<PaymentResponse>.Failure(
                $"Сумма платежа ({request.Amount.ToMoney()}) превышает долг магазина ({balance.Debt.ToMoney()}).");
        }

        Payment payment;
        try
        {
            payment = Payment.Create(shop.Id, request.OrderId, request.Amount, request.Method, caller.Id);
        }
        catch (DomainException ex)
        {
            return Result<PaymentResponse>.Failure(ex.Message);
        }

        await _paymentRepository.AddAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await ReloadAsync(payment.Id, ct);
    }

    public async Task<Result<PaymentResponse>> ReverseAsync(
        Guid id, ReversePaymentRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        if (caller.Role != UserRole.Admin)
            return Result<PaymentResponse>.Failure("Недостаточно прав.", ErrorType.Forbidden);

        var payment = await _paymentRepository.GetByIdAsync(id, ct);
        if (payment is null)
            return Result<PaymentResponse>.Failure("Платёж не найден.", ErrorType.NotFound);

        if (payment.IsReversal)
            return Result<PaymentResponse>.Failure("Сторно нельзя сторнировать повторно.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        await _shopRepository.LockForUpdateAsync(payment.ShopId, ct);

        if (await _paymentRepository.IsReversedAsync(id, ct))
            return Result<PaymentResponse>.Failure("Этот платёж уже сторнирован.", ErrorType.Conflict);

        Payment reversal;
        try
        {
            reversal = payment.CreateReversal(caller.Id, request.Reason);
        }
        catch (DomainException ex)
        {
            return Result<PaymentResponse>.Failure(ex.Message);
        }

        await _paymentRepository.AddAsync(reversal, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await ReloadAsync(reversal.Id, ct);
    }

    private async Task<Result<PaymentResponse>> ReloadAsync(Guid id, CancellationToken ct)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, ct);

        return Result<PaymentResponse>.Success(ToResponse(payment!, false));
    }

    private static PaymentResponse ToResponse(Payment payment, bool isReversed) => new()
    {
        Id = payment.Id,
        ShopId = payment.ShopId,
        ShopName = payment.Shop?.Name ?? string.Empty,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Method = payment.Method.ToString(),
        ReceivedByUserId = payment.ReceivedByUserId,
        ReceivedByName = payment.ReceivedByUser?.FullName,
        IsReversal = payment.IsReversal,
        IsReversed = isReversed,
        ReversalOfPaymentId = payment.ReversalOfPaymentId,
        ReversalReason = payment.ReversalReason,
        CreatedAt = payment.CreatedAt
    };
}