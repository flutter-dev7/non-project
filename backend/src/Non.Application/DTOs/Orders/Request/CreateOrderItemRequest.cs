using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Orders.Request;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; }
}