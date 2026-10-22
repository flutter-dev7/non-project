using Non.Application.Common;
using Non.Application.DTOs.Products.Request;
using Non.Application.DTOs.Products.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;

namespace Non.Application.Services;

public class ProductService : IProductService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<IReadOnlyList<ProductResponse>>> GetActiveAsync(CancellationToken ct = default)
    {
        var products = await _cacheService.GetOrCreateAsync<IReadOnlyList<ProductResponse>>(
            CacheKeys.ActiveProducts,
            async () => (await _productRepository.GetActiveAsync(ct)).Select(ToResponse).ToList(),
            CacheDuration);

        return Result<IReadOnlyList<ProductResponse>>.Success(products);
    }

    public async Task<Result<PagedResult<ProductResponse>>> GetPagedAsync(
        ProductQueryRequest request, CancellationToken ct = default)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            request.Search, request.IsActive, page, pageSize, ct);

        var result = new PagedResult<ProductResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Result<PagedResult<ProductResponse>>.Success(result);
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Product(id);

        var cached = await _cacheService.GetAsync<ProductResponse>(key);
        if (cached is not null)
            return Result<ProductResponse>.Success(cached);

        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductResponse>.Failure("Продукт не найден.", ErrorType.NotFound);

        var response = ToResponse(product);
        await _cacheService.SetAsync(key, response, CacheDuration);

        return Result<ProductResponse>.Success(response);
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _productRepository.ExistsByNameAsync(name, null, ct))
            return Result<ProductResponse>.Failure("Продукт с таким названием уже существует.", ErrorType.Conflict);

        var product = new Product
        {
            Name = name,
            WeightInGrams = request.WeightInGrams,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _cacheService.RemoveAsync(CacheKeys.ActiveProducts);

        return Result<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(
        Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductResponse>.Failure("Продукт не найден.", ErrorType.NotFound);

        var name = request.Name.Trim();

        if (await _productRepository.ExistsByNameAsync(name, id, ct))
            return Result<ProductResponse>.Failure("Продукт с таким названием уже существует.", ErrorType.Conflict);

        product.Name = name;
        product.WeightInGrams = request.WeightInGrams;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(ct);

        await _cacheService.RemoveAsync(CacheKeys.ActiveProducts);
        await _cacheService.RemoveAsync(CacheKeys.Product(id));

        return Result<ProductResponse>.Success(ToResponse(product));
    }

    private static ProductResponse ToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        WeightInGrams = product.WeightInGrams,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        IsActive = product.IsActive
    };
}