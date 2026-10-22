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
            ReversalReason = reason
        };
    }
}