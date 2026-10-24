using Non.Domain.Common;
using Non.Domain.Enums;

namespace Non.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    public Guid ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }

    public Guid? ReversalOfPaymentId { get; set; }
    public Payment? ReversalOfPayment { get; set; }
    public string? ReversalReason { get; set; }

    public bool IsReversal => ReversalOfPaymentId.HasValue;

    public static Payment Create(
        Guid shopId, Guid? orderId, decimal amount, PaymentMethod method, Guid receivedByUserId)
    {
        if (amount <= 0)
            throw new DomainException("Сумма платежа должна быть больше нуля.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Сумма платежа может содержать не более двух знаков после запятой.");

        return new Payment
        {
            ShopId = shopId,
            OrderId = orderId,
            Amount = amount,
            Method = method,
            ReceivedByUserId = receivedByUserId
        };
    }

    public Payment CreateReversal(Guid userId, string reason)
    {
        if (IsReversal)
            throw new DomainException("Сторно нельзя сторнировать повторно.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Нужно указать причину сторно.");

        return new Payment
        {
            ShopId = ShopId,
            OrderId = OrderId,
            Amount = -Amount,
            Method = Method,
            ReceivedByUserId = userId,
            ReversalOfPaymentId = Id,
            ReversalReason = reason.Trim()
        };
    }
}