namespace Non.Application.DTOs.Debts.Response;

public class DebtListItemResponse
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopPhone { get; set; } = string.Empty;
    public decimal Debt { get; set; }
    public decimal? DebtLimit { get; set; }
    public bool IsOverLimit { get; set; }
}