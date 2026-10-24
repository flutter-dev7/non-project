using Non.Application.Common;

namespace Non.Application.DTOs.Payments.Request;

public class PaymentQueryRequest : PaginationRequest
{
    public Guid? ShopId { get; set; }
}