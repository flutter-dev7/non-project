namespace Non.Application.DTOs.Visits.Response;

public class VisitResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public Guid AgentId { get; set; }
    public string? AgentName { get; set; }
    public DateTime VisitedAt { get; set; }
    public string? Note { get; set; }
}