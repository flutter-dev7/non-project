namespace Non.Application.DTOs.Reports.Response;

public class ProductionItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class ProductionReportResponse
{
    public DateOnly Date { get; set; }
    public int OrdersCount { get; set; }
    public int TotalQuantity { get; set; }
    public List<ProductionItemResponse> Items { get; set; } = new();
}