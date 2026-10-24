using System.ComponentModel.DataAnnotations;
using Non.Domain.Enums;

namespace Non.Application.DTOs.Orders.Request;

public class ChangeOrderStatusRequest
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }

    [Range(typeof(decimal), "0.01", "100000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? PaymentAmount { get; set; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod? PaymentMethod { get; set; }
}