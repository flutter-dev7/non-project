namespace Non.Application.DTOs.Debts.Response;

public class UnpaidOrderResponse
{
    public Guid OrderId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}