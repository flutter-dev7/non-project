using Non.Application.Common;

namespace Non.Application.DTOs.Visits.Request;

public class VisitQueryRequest : PaginationRequest
{
    public Guid? ShopId { get; set; }
    public Guid? AgentId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}