using Non.Application.Common;
using Non.Application.DTOs.Reports.Request;
using Non.Application.DTOs.Reports.Response;

namespace Non.Application.Interfaces.Services;

public interface IReportService
{
    Task<Result<ProductionReportResponse>> GetProductionAsync(
        ProductionReportRequest request, CancellationToken ct = default);

    Task<Result<SalesReportResponse>> GetSalesAsync(
        SalesReportRequest request, CancellationToken ct = default);
}