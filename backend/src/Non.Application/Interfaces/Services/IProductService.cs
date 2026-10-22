using Non.Application.Common;
using Non.Application.DTOs.Products.Request;
using Non.Application.DTOs.Products.Response;

namespace Non.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<IReadOnlyList<ProductResponse>>> GetActiveAsync(CancellationToken ct = default);

    Task<Result<PagedResult<ProductResponse>>> GetPagedAsync(ProductQueryRequest request, CancellationToken ct = default);

    Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
}