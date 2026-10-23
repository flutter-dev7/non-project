using Non.Application.Common;
using Non.Domain.Enums;

namespace Non.Application.DTOs.Users.Request;

public class UserQueryRequest : PaginationRequest
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}