using System.ComponentModel.DataAnnotations;
using Non.Domain.Enums;

namespace Non.Application.DTOs.Orders.Request;

public class ChangeOrderStatusRequest
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}