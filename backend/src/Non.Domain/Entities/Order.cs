using Non.Domain.Common;
using Non.Domain.Enums;

namespace Non.Domain.Entities;

public class Order : BaseEntity
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.New] = new[] { OrderStatus.Accepted, OrderStatus.Cancelled },
        [OrderStatus.Accepted] = new[] { OrderStatus.Baking, OrderStatus.Cancelled },
        [OrderStatus.Baking] = new[] { OrderStatus.OnTheWay },
        [OrderStatus.OnTheWay] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public OrderSource Source { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;
    public DateOnly DeliveryDate { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal TotalAmount { get; set; }

    public Guid? CourierId { get; set; }
    public User? Courier { get; set; }

    public string? Note { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    public bool CanBeCancelled => Status is OrderStatus.New or OrderStatus.Accepted;

    public bool CountsAsDebt => Status == OrderStatus.Delivered && PaymentType == PaymentType.Debt;

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Количество должно быть больше нуля.");
        if (unitPrice < 0)
            throw new DomainException("Цена не может быть отрицательной.");

        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Items.Add(new OrderItem
            {
                OrderId = Id,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }

        RecalculateTotal();
    }

    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.LineTotal);
    }

    public void RecordStatus(Guid changedByUserId)
    {
        StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            Status = Status,
            ChangedByUserId = changedByUserId
        });
    }

    public void ChangeStatus(OrderStatus newStatus, Guid changedByUserId)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainException($"Нельзя изменить статус заказа с {Status} на {newStatus}.");

        Status = newStatus;
        RecordStatus(changedByUserId);
    }

    public void Cancel(Guid changedByUserId)
    {
        if (!CanBeCancelled)
            throw new DomainException("Заказ нельзя отменить после начала выпечки.");

        Status = OrderStatus.Cancelled;
        RecordStatus(changedByUserId);
    }
}