using Non.Application.Common;
using Non.Application.DTOs.Payments.Request;
using Non.Application.DTOs.Payments.Response;

namespace Non.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<PagedResult<PaymentResponse>>> GetPagedAsync(
        PaymentQueryRequest request, CurrentUser caller, CancellationToken ct = default);

    Task<Result<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request, CurrentUser caller, CancellationToken ct = default);

    Task<Result<PaymentResponse>> ReverseAsync(
        Guid id, ReversePaymentRequest request, CurrentUser caller, CancellationToken ct = default);
}