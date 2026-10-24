using Non.Application.Common;
using Non.Application.DTOs.Reports.Request;
using Non.Application.DTOs.Reports.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.Application.Services;

public class ReportService : IReportService
{
    private const int MaxPeriodDays = 366;

    private readonly IReportRepository _reportRepository;
    private readonly TimeProvider _timeProvider;

    public ReportService(IReportRepository reportRepository, TimeProvider timeProvider)
    {
        _reportRepository = reportRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ProductionReportResponse>> GetProductionAsync(
        ProductionReportRequest request, CancellationToken ct = default)
    {
        var date = request.Date ?? Today().AddDays(1);

        var ordersCount = await _reportRepository.CountOrdersForProductionAsync(date, ct);
        var rows = await _reportRepository.GetProductionRowsAsync(date, ct);

        return Result<ProductionReportResponse>.Success(new ProductionReportResponse
        {
            Date = date,
            OrdersCount = ordersCount,
            TotalQuantity = rows.Sum(r => r.Quantity),
            Items = rows
                .OrderByDescending(r => r.Quantity)
                .ThenBy(r => r.ProductName)
                .Select(r => new ProductionItemResponse
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    Quantity = r.Quantity
                })
                .ToList()
        });
    }

    public async Task<Result<SalesReportResponse>> GetSalesAsync(
        SalesReportRequest request, CancellationToken ct = default)
    {
        var from = request.From!.Value;
        var to = request.To!.Value;

        if (to < from)
            return Result<SalesReportResponse>.Failure("Дата «по» не может быть раньше даты «с».");

        if (to.DayNumber - from.DayNumber + 1 > MaxPeriodDays)
            return Result<SalesReportResponse>.Failure($"Период не может быть больше {MaxPeriodDays} дней.");

        var fromUtc = ToUtcStart(from);
        var toUtc = ToUtcStart(to.AddDays(1));

        var byDay = await _reportRepository.GetSalesByDayAsync(from, to, ct);
        var byProduct = await _reportRepository.GetSalesByProductAsync(from, to, ct);
        var received = await _reportRepository.GetPaymentsTotalAsync(fromUtc, toUtc, ct);

        return Result<SalesReportResponse>.Success(new SalesReportResponse
        {
            From = from,
            To = to,
            OrdersCount = byDay.Sum(d => d.OrdersCount),
            TotalQuantity = byProduct.Sum(p => p.Quantity),
            TotalAmount = byDay.Sum(d => d.Amount),
            ReceivedPayments = received,
            ByDay = byDay
                .OrderBy(d => d.Date)
                .Select(d => new SalesDayResponse
                {
                    Date = d.Date,
                    OrdersCount = d.OrdersCount,
                    Amount = d.Amount
                })
                .ToList(),
            ByProduct = byProduct
                .OrderByDescending(p => p.Amount)
                .ThenBy(p => p.ProductName)
                .Select(p => new SalesProductResponse
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Quantity = p.Quantity,
                    Amount = p.Amount
                })
                .ToList()
        });
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(_timeProvider.GetUtcNow().ToOffset(BusinessRules.UtcOffset).DateTime);

    private static DateTime ToUtcStart(DateOnly localDate) =>
        DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue) - BusinessRules.UtcOffset, DateTimeKind.Utc);
}