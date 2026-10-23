using Non.Application.Common;
using Non.Application.DTOs.Shops.Request;
using Non.Application.DTOs.Shops.Response;

namespace Non.Application.Interfaces.Services;

public interface IShopService
{
    Task<Result<PagedResult<ShopResponse>>> GetPagedAsync(ShopQueryRequest request, CancellationToken ct = default);

    Task<Result<ShopResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<ShopResponse>> GetMyAsync(Guid userId, CancellationToken ct = default);

    Task<Result<ShopResponse>> CreateAsync(CreateShopRequest request, CancellationToken ct = default);

    Task<Result<ShopResponse>> CreateMyAsync(Guid userId, CreateShopRequest request, CancellationToken ct = default);

    Task<Result<ShopResponse>> UpdateAsync(Guid id, UpdateShopRequest request, CancellationToken ct = default);

    Task<Result<ShopResponse>> SetOwnerAsync(Guid id, SetOwnerRequest request, CancellationToken ct = default);

    Task<Result<ShopResponse>> SetDebtLimitAsync(Guid id, SetDebtLimitRequest request, CancellationToken ct = default);
}