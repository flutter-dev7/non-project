namespace Non.Application.DTOs.Payments.Response;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public Guid ReceivedByUserId { get; set; }
    public string? ReceivedByName { get; set; }
    public bool IsReversal { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversalOfPaymentId { get; set; }
    public string? ReversalReason { get; set; }
    public DateTime CreatedAt { get; set; }
}