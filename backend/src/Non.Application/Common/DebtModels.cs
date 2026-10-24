namespace Non.Application.Common;

public record DeliveredOrderInfo(Guid Id, DateOnly DeliveryDate, DateTime CreatedAt, decimal TotalAmount);

public record OrderPaymentStatus(Guid OrderId, DateOnly DeliveryDate, decimal TotalAmount, decimal PaidAmount)
{
    public decimal RemainingAmount => TotalAmount - PaidAmount;
}

public record ShopBalance(decimal DeliveredTotal, decimal PaidTotal, decimal PendingDebtOrdersTotal)
{
    public decimal Debt => DeliveredTotal - PaidTotal;
}