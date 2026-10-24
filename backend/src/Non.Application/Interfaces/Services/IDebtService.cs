using Non.Application.Common;
using Non.Application.DTOs.Debts.Request;
using Non.Application.DTOs.Debts.Response;

namespace Non.Application.Interfaces.Services;

public interface IDebtService
{
    Task<ShopBalance> GetBalanceAsync(Guid shopId, CancellationToken ct = default);

    Task<Result<ShopBalanceResponse>> GetShopBalanceAsync(Guid shopId, CancellationToken ct = default);

    Task<Result<ShopBalanceResponse>> GetMyBalanceAsync(CurrentUser caller, CancellationToken ct = default);

    Task<Result<PagedResult<DebtListItemResponse>>> GetDebtsAsync(DebtQueryRequest request, CancellationToken ct = default);
}