using Non.Application.Common;

namespace Non.Application.DTOs.Shops.Request;

public class ShopQueryRequest : PaginationRequest
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}