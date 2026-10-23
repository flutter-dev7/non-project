namespace Non.Application.DTOs.Orders.Response;

public class OrderDetailsResponse : OrderResponse
{
    public List<OrderStatusHistoryResponse> History { get; set; } = new();
}