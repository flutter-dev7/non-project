namespace Non.Application.DTOs.Orders.Response;

public class OrderStatusHistoryResponse
{
    public string Status { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
}