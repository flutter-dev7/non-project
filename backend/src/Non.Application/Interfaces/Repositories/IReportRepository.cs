using Non.Application.Common;

namespace Non.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<int> CountOrdersForProductionAsync(DateOnly date, CancellationToken ct = default);

    Task<List<ProductionRow>> GetProductionRowsAsync(DateOnly date, CancellationToken ct = default);

    Task<List<SalesDayRow>> GetSalesByDayAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<List<SalesProductRow>> GetSalesByProductAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<decimal> GetPaymentsTotalAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}