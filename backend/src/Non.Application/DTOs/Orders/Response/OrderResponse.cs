namespace Non.Application.DTOs.Orders.Response;

public class OrderResponse
{
    public Guid Id { get; set; }

    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string ShopPhone { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateOnly DeliveryDate { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }

    public string Source { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OrderItemResponse> Items { get; set; } = new();
}