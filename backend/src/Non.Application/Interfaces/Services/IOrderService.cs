using Non.Application.Common;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;

namespace Non.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<PagedResult<OrderResponse>>> GetPagedAsync(
        OrderQueryRequest request, CurrentUser caller, CancellationToken ct = default);

    Task<Result<OrderDetailsResponse>> GetByIdAsync(Guid id, CurrentUser caller, CancellationToken ct = default);

    Task<Result<OrderDetailsResponse>> CreateAsync(
        CreateOrderRequest request, CurrentUser caller, CancellationToken ct = default);

    Task<Result<OrderDetailsResponse>> ChangeStatusAsync(
        Guid id, ChangeOrderStatusRequest request, CurrentUser caller, CancellationToken ct = default);

    Task<Result<OrderDetailsResponse>> CancelAsync(Guid id, CurrentUser caller, CancellationToken ct = default);

    Task<Result<OrderDetailsResponse>> AssignCourierAsync(
        Guid id, AssignCourierRequest request, CancellationToken ct = default);
}