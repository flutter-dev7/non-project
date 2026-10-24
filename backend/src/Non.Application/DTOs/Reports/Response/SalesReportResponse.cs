namespace Non.Application.DTOs.Reports.Response;

public class SalesDayResponse
{
    public DateOnly Date { get; set; }
    public int OrdersCount { get; set; }
    public decimal Amount { get; set; }
}

public class SalesProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class SalesReportResponse
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int OrdersCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedPayments { get; set; }
    public List<SalesDayResponse> ByDay { get; set; } = new();
    public List<SalesProductResponse> ByProduct { get; set; } = new();
}