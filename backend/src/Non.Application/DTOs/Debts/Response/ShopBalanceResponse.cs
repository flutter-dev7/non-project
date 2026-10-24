namespace Non.Application.DTOs.Debts.Response;

public class ShopBalanceResponse
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal DeliveredTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal Debt { get; set; }
    public decimal PendingDebtOrdersTotal { get; set; }
    public decimal? DebtLimit { get; set; }
    public List<UnpaidOrderResponse> UnpaidOrders { get; set; } = new();
}