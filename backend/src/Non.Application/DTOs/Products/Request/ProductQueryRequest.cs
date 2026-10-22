using Non.Application.Common;

namespace Non.Application.DTOs.Products.Request;

public class ProductQueryRequest : PaginationRequest
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}