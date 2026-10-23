using Non.Domain.Enums;

namespace Non.Application.Common;

public class OrderFilter
{
    public Guid? ShopId { get; set; }
    public Guid? CourierId { get; set; }
    public OrderStatus? Status { get; set; }
    public DateOnly? DeliveryDateFrom { get; set; }
    public DateOnly? DeliveryDateTo { get; set; }
}