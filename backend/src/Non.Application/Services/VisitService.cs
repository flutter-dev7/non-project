using Non.Application.Common;
using Non.Application.DTOs.Visits.Request;
using Non.Application.DTOs.Visits.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class VisitService : IVisitService
{
    private readonly IVisitRepository _visitRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public VisitService(
        IVisitRepository visitRepository,
        IShopRepository shopRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _visitRepository = visitRepository;
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<VisitResponse>>> GetPagedAsync(
        VisitQueryRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        if (caller.Role is not (UserRole.Admin or UserRole.Agent))
            return Result<PagedResult<VisitResponse>>.Failure("Недостаточно прав.", ErrorType.Forbidden);

        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateTo < request.DateFrom)
            return Result<PagedResult<VisitResponse>>.Failure("Дата «по» не может быть раньше даты «с».");

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var agentId = caller.Role == UserRole.Agent ? caller.Id : request.AgentId;
        var fromUtc = request.DateFrom.HasValue ? ToUtcStart(request.DateFrom.Value) : (DateTime?)null;
        var toUtc = request.DateTo.HasValue ? ToUtcStart(request.DateTo.Value.AddDays(1)) : (DateTime?)null;

        var (items, totalCount) = await _visitRepository.GetPagedAsync(
            request.ShopId, agentId, fromUtc, toUtc, page, pageSize, ct);

        return Result<PagedResult<VisitResponse>>.Success(new PagedResult<VisitResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<VisitResponse>> GetByIdAsync(Guid id, CurrentUser caller, CancellationToken ct = default)
    {
        if (caller.Role is not (UserRole.Admin or UserRole.Agent))
            return Result<VisitResponse>.Failure("Недостаточно прав.", ErrorType.Forbidden);

        var visit = await _visitRepository.GetDetailsByIdAsync(id, ct);
        if (visit is null || (caller.Role == UserRole.Agent && visit.AgentId != caller.Id))
            return Result<VisitResponse>.Failure("Визит не найден.", ErrorType.NotFound);

        return Result<VisitResponse>.Success(ToResponse(visit));
    }

    public async Task<Result<VisitResponse>> CreateAsync(
        CreateVisitRequest request, CurrentUser caller, CancellationToken ct = default)
    {
        if (caller.Role is not (UserRole.Admin or UserRole.Agent))
            return Result<VisitResponse>.Failure("Недостаточно прав.", ErrorType.Forbidden);

        var shop = await _shopRepository.GetDetailsByIdAsync(request.ShopId, ct);
        if (shop is null)
            return Result<VisitResponse>.Failure("Магазин не найден.", ErrorType.NotFound);

        if (!shop.IsActive)
            return Result<VisitResponse>.Failure("Магазин отключён.");

        var visit = new Visit
        {
            ShopId = shop.Id,
            AgentId = caller.Id,
            VisitedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        await _visitRepository.AddAsync(visit, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var created = await _visitRepository.GetDetailsByIdAsync(visit.Id, ct);

        return Result<VisitResponse>.Success(ToResponse(created!));
    }

    private static DateTime ToUtcStart(DateOnly localDate) =>
        DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue) - BusinessRules.UtcOffset, DateTimeKind.Utc);

    private static VisitResponse ToResponse(Visit visit) => new()
    {
        Id = visit.Id,
        ShopId = visit.ShopId,
        ShopName = visit.Shop?.Name ?? string.Empty,
        ShopAddress = visit.Shop?.Address ?? string.Empty,
        AgentId = visit.AgentId,
        AgentName = visit.Agent?.FullName,
        VisitedAt = visit.VisitedAt,
        Note = visit.Note
    };
}