using Non.Domain.Common;

namespace Non.Domain.Entities;

public class Visit : BaseEntity
{
    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public Guid AgentId { get; set; }
    public User? Agent { get; set; }

    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}